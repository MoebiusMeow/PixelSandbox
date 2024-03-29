using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Weapons
{
    public class SandBomb : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 99;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DryBomb);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<SandBombProjectile>();
        }

        public override void AddRecipes()
        {
            CreateRecipe(2).AddIngredient(ItemID.Bomb, 2).AddIngredient(ModContent.ItemType<SandBag>()).Register();
            CreateRecipe(1).AddIngredient(ItemID.Bomb, 1).AddIngredient(ModContent.ItemType<SandBag>()).Register();
        }
    }

    public class SandBombProjectile : PSModProjectile
    {
        public override string Texture => (typeof(SandBomb).Namespace + "." + "SandBomb").Replace('.', '/');

        public SandBombProjectile()
        {
            Behavior.Update = GenerateSand;
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.DryBomb);
            // Projectile.aiStyle = -1;
            Projectile.timeLeft = 120;
            Projectile.width = 22;
            Projectile.height = 26;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }

        public override void AI()
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 3)
            {
                Projectile.Resize(48, 48);
                Projectile.damage = 100;
                Projectile.knockBack = 12f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        protected Vector4[] data;

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
                        if ((uv - Vector2.One * 0.5f).Length() > 0.5f || !Main.rand.NextBool(8))
                            continue;
                        Vector4 value = data[i + j * effectSize.X];
                        if (value.W == 0)
                        {
                            data[i + j * effectSize.X] = new Vector4(
                                Color.Lerp(new Color(212, 192, 100), Color.Orange, 0)
                                .ToVector3() * MathHelper.Lerp(0.9f, 1.0f, Main.rand.NextFloat()), 1);
                        }
                    }
                effectRT.SetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
            };
            var sandProjectile = sandboxObject as SandBombProjectile;
            float radius = 120;
            Vector2 targetPos = sandProjectile.Projectile.Center;
            targetPos -= Vector2.One * radius;

            if (sandProjectile.Projectile.timeLeft > 10)
            {
                Vector4 hitSand = PSSandboxSystem.Instance.SoftSampleSand(sandProjectile.Projectile.Center);
                if (hitSand.W > 0)
                    sandProjectile.Projectile.timeLeft = 10;
            }
            if (sandProjectile.Projectile.timeLeft <= 10 && sandProjectile.Projectile.localAI[1] == 0)
            {
                sandProjectile.Projectile.localAI[1] = 1;
                if (sandProjectile.Projectile.owner == Main.myPlayer)
                    for (int i = 0; i < 2; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_ReleaseEntity(), Projectile.Center, Main.rand.NextVector2FromRectangle(new Rectangle(-10, -12, 20, 3)),
                            ModContent.ProjectileType<SandBagProjectile>(), 0, 0, Projectile.owner);
                    }
                for (int i = 0; i < 15; i++)
                {
                    var dust = Dust.NewDustDirect(sandProjectile.Projectile.position, sandProjectile.Projectile.width, sandProjectile.Projectile.height, DustID.Smoke,
                        0, 0, 0, default, 1.5f);
                    dust.velocity = (dust.velocity + sandProjectile.Projectile.velocity).SafeNormalize(Vector2.Zero) * 1.4f;
                }
                for (int i = 0; i < 15; i++)
                {
                    var dust = Dust.NewDustDirect(sandProjectile.Projectile.position, sandProjectile.Projectile.width, sandProjectile.Projectile.height, DustID.Smoke,
                        0, 0, 0, default, 1.5f);
                    dust.velocity *= 1.4f;
                }
                for (int i = 0; i < 30; i++)
                {
                    var dust = Dust.NewDustDirect(sandProjectile.Projectile.position, sandProjectile.Projectile.width, sandProjectile.Projectile.height, DustID.Smoke,
                        0, 0, 0, default, 1.2f);
                    dust.velocity *= 7.0f;
                }
                for (int i = 0; i < 30; i++)
                {
                    var dust = Dust.NewDustDirect(sandProjectile.Projectile.position, sandProjectile.Projectile.width, sandProjectile.Projectile.height, DustID.Smoke,
                        0, 0, 0, default, 0.3f);
                    dust.velocity *= 4.0f;
                }
                PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * radius * 2, callback);
                sandProjectile.Projectile.timeLeft = 3;
                SoundEngine.PlaySound(SoundID.Item14, sandProjectile.Projectile.Center);
            }
        }
    }
}