using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PixelSandbox.Contents.Items.Cleaners
{
    public class VacuumCleanerBone : VacuumCleanerBase
    {
        public override string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public override SoundStyle EndSound => SoundID.Item32;

        public override float CleanerRadius => 100;
        public override float Centrifuge => 0.4f;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useStyle = ItemUseStyleID.MowTheLawn;
            Item.noMelee = true;
            Item.value = 1;
            Item.rare = ItemRarityID.Purple;
            // Item.UseSound = SoundID.Item90 with { Pitch = -0.1f };
            Item.UseSound = SoundID.DD2_DarkMageHealImpact with { Pitch = -0.1f, Volume = 1.5f };
            Item.autoReuse = true;
            Item.useTurn = true;
            sandCount = 0;
            Item.shoot = ModContent.ProjectileType<BoneCleanerProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 15);
            recipe.AddIngredient(ItemID.JungleSpores, 15);
            recipe.AddIngredient(ItemID.ShadowScale, 15);
            recipe.AddIngredient(ModContent.ItemType<SandBag>());
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();

            recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 15);
            recipe.AddIngredient(ItemID.JungleSpores, 15);
            recipe.AddIngredient(ItemID.TissueSample, 15);
            recipe.AddIngredient(ModContent.ItemType<SandBag>());
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }

    public class BoneCleanerProjectile : PSModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.DD2DarkMageRaise}";

        public BoneCleanerProjectile() => Behavior.Update = GenerateSand;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.WaterBolt);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 160;
            Projectile.width = Projectile.height = 82;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            DrawOriginOffsetX = 0;
            DrawOriginOffsetY = 0;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Main.rand.NextFloatDirection();
            Projectile.scale = 0.1f;
            base.OnSpawn(source);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            lightColor *= 0.6f;
            return base.PreDraw(ref lightColor);
        }

        public override void PostDraw(Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            base.PostDraw(lightColor);
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(1, 0.1f, 0.8f) * 0.5f);
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.DemonTorch, 0, 0, 0, default, 2);
                dust.velocity = (dust.position - Projectile.Center).SafeNormalize(Vector2.Zero) * 1f;
            }
            Projectile.rotation += 5e-5f * MathF.Pow(Projectile.timeLeft, 2);
            Projectile.alpha = Math.Max(0, 255 - 10 * Projectile.timeLeft);
            Projectile.scale = MathHelper.Lerp(Projectile.scale, Projectile.timeLeft < 20 ? 2 : 1, 0.1f);
            if (Projectile.timeLeft == 20)
                SoundEngine.PlaySound(SoundID.Item51 with { Volume = 0.15f, Pitch = -0.5f }, Projectile.Center);
            base.AI();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        protected Vector4[] data, newData;
        protected int[] dupCount, dupLast;

        public void GenerateSand(ISandboxObject sandboxObject)
        {
            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;

                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                var effectSize = PSSandboxSystem.Instance.effectSize;

                if (data == null || data.Length != effectSize.X * effectSize.Y)
                    data = new Vector4[effectSize.X * effectSize.Y];

                effectRTSwap.GetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
                for (int j = 0; j < effectSize.Y; j++)
                    for (int i = 0; i < effectSize.X; i++)
                    {
                        Vector2 uv = new Vector2(i / (float)effectSize.X, j / (float)effectSize.Y);
                        Vector4 value = data[i + j * effectSize.X];
                        if (value.W == 0)
                        {
                            data[i + j * effectSize.X] = new Vector4(
                                Color.Purple.ToVector3() * MathHelper.Lerp(0.3f, 1.0f, Main.rand.NextFloat()), 1);
                        }
                    }
                effectRT.SetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
            };
            float radius = 4;
            if (sandboxObject is not BoneCleanerProjectile proj)
                return;
            Player player = Main.player[proj.Projectile.owner];
            proj.Projectile.Center = player.Bottom + new Vector2(player.Directions.X * 10, -18 - 20) + Vector2.UnitY * player.gfxOffY;
            Vector2 targetPos = proj.Projectile.Center;
            // targetPos -= Vector2.One * radius;

            if (proj.Projectile.timeLeft <= 10)
                PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * radius * 2, callback);

        }
    }
}