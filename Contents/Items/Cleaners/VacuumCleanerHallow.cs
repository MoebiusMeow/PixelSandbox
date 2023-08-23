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
    public class VacuumCleanerHallow : VacuumCleanerBase
    {
        public override string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public override SoundStyle EndSound => SoundID.Item32;

        public override float CleanerRadius => 60;
        public override float Centrifuge => 0.8f;

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
            Item.shoot = ModContent.ProjectileType<HallowCleanerProjectile>();
            Item.shootSpeed = 4;
        }

        public override void Vacuum(ISandboxObject sandboxObject, Player player)
        {
            base.Vacuum(sandboxObject, player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectileDirect(Item.GetSource_ReleaseEntity(), player.position, Vector2.Zero, ModContent.ProjectileType<HallowCleanerGlow>(), 0, 0, player.whoAmI)
                    .netUpdate = true;
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Texture2D glowTexture = ModContent.Request<Texture2D>((GetType().Namespace + "." + Name + "_glow").Replace('.', '/')).Value;
            Main.spriteBatch.Draw(glowTexture, Item.position - Main.screenPosition + new Vector2(Item.width, Item.height) * 0.5f, null, Color.White, rotation, new Vector2(Item.width, Item.height) * 0.5f, scale, SpriteEffects.None, 0);
            base.PostDrawInWorld(spriteBatch, lightColor, alphaColor, rotation, scale, whoAmI);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LightningBug, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 15);
            recipe.AddIngredient(ItemID.HallowedBar, 15);
            recipe.AddIngredient(ModContent.ItemType<SandBag>());
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }


    public class HallowCleanerGlow : PSModProjectile
    {
        public override string Texture => (GetType().Namespace + ".VacuumCleanerHallow_glow").Replace('.', '/');

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.WaterBolt);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 500;
            Projectile.width = 32;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            DrawOriginOffsetX = 0;
            DrawOriginOffsetY = 0;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            Projectile.hide = true;
            overPlayers.Add(index);
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White;
            Player player = Main.player[Projectile.owner];
            if (player.active && player.HeldItem?.ModItem is VacuumCleanerHallow cleaner)
            {
                Projectile.position = player.itemLocation - cleaner.Item.Size + Vector2.UnitX * Projectile.width * player.direction;
                if (player.direction < 0)
                    Projectile.position.X += 22;
                Projectile.position.X = MathF.Floor(Projectile.position.X);
                Projectile.rotation = player.itemRotation + player.fullRotation;
                Projectile.Center = (Projectile.Center - (player.MountedCenter - player.Size * 0.5f + player.fullRotationOrigin))
                    .RotatedBy(player.fullRotation) + player.MountedCenter - player.Size * 0.5f + player.fullRotationOrigin;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
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
            Player player = Main.player[Projectile.owner];
            if (!player.active || (player.heldProj != -1 && player.heldProj != Projectile.whoAmI) || player.HeldItem?.ModItem is not VacuumCleanerHallow cleaner)
            {
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 2);
                if (player.heldProj != -1)
                {
                    Main.projectile[player.heldProj].timeLeft = 500;
                    if (player.whoAmI == Main.myPlayer)
                        Main.projectile[player.heldProj].netUpdate = true;
                }
                return;
            }
            player.heldProj = Projectile.whoAmI;
            Projectile.alpha = (int)(255 * (1 - Projectile.timeLeft / 50f));
            if (Projectile.timeLeft == 50)
            {
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.15f, Pitch = -0.5f }, Projectile.Center);
                for (int i = 0; i < 4; i++)
                {
                    var dust = Dust.NewDustDirect(Projectile.Center + new Vector2(3 + 1 * player.direction, -6), 0, 0, DustID.GoldCoin, 0, 0, 0, default, 0.5f);
                    dust.velocity = dust.velocity.SafeNormalize(Vector2.Zero) * 0.6f + player.velocity;
                }
            }
            else if (Projectile.timeLeft > 50 && Main.rand.NextBool(10))
            {
                var dust = Dust.NewDustDirect(Projectile.Center + new Vector2(3 + 1 * player.direction, -6), 0, 0, DustID.GoldCoin, 0, 0, 0, default, 0.5f);
                dust.velocity = dust.velocity.SafeNormalize(Vector2.Zero) * 0.4f;
            }
            base.AI();
        }
    }

    public class HallowCleanerProjectile : PSModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WhiteTigerPounce}";

        public HallowCleanerProjectile() => Behavior.Update = Vacuum;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.WaterBolt);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 70;
            Projectile.width = Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            DrawOriginOffsetX = 0;
            DrawOriginOffsetY = 0;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Main.rand.NextFloatDirection();
            Projectile.scale = 0.1f;
            Projectile.velocity = Projectile.velocity.RotatedBy(-MathF.PI * 0.5f);
            base.OnSpawn(source);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            BlendState blend = BlendState.Additive;
            blend.ColorBlendFunction = BlendFunction.ReverseSubtract;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, blend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            lightColor *= 0.6f;
            return base.PreDraw(ref lightColor);
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D asset = TextureAssets.Extra[89].Value;
            Vector2[] oldPos = Projectile.oldPos;
            List<Vector2> pos = new List<Vector2>();
            for (int i = oldPos.Length - 1; i >= 0; i--)
            {
                if (oldPos[i] == Vector2.Zero)
                    continue;
                pos.Add(oldPos[i]);
                if (i == 0)
                    continue;
                for (int k = 1; k < 3; k++)
                    pos.Add(Vector2.Lerp(oldPos[i], oldPos[i - 1], k / 3f));
            }
            for (int i = 1; i < pos.Count; i++)
            {
                Main.spriteBatch.Draw(asset,
                    pos[i] - Main.screenPosition + Projectile.Size * 0.5f, null,
                    new Color(1.0f, 0.8f, 0.4f) * ((255f - Projectile.alpha) / 255f) * 0.8f * MathHelper.Lerp(0, 1, i / (float)pos.Count),
                    (pos[i - 1] - pos[i]).ToRotation() + 0.5f * MathF.PI,
                    asset.Size() * 0.5f, 1.0f, SpriteEffects.None, 0);
            }

            float realCleanerRadius = 260.0f * (Projectile.timeLeft / 70f) * MathF.Min(1, (70 - Projectile.timeLeft) * 0.1f);
            asset = TextureAssets.Extra[174].Value;
            Main.spriteBatch.Draw(asset,
                pos[^1] - Main.screenPosition + Projectile.Size * 0.5f, null,
                new Color(1.0f, 0.8f, 0.4f) * ((255f - Projectile.alpha) / 255f) * 0.8f * 1, 0, asset.Size() * 0.5f,
                realCleanerRadius * 2 * 0.4f / asset.Width, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            base.PostDraw(lightColor);
        }

        public override void AI()
        {
            if (Main.rand.NextBool(1))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0, 0, 0, default, 2);
                dust.velocity = (dust.position - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.5f;
            }
            Projectile.rotation += 5e-5f * MathF.Pow(Projectile.timeLeft, 2);
            Projectile.velocity = Projectile.velocity.RotatedBy(0.05f);
            Projectile.alpha = Math.Max(0, 255 - 10 * Projectile.timeLeft);
            // Projectile.scale = MathHelper.Lerp(Projectile.scale, Projectile.timeLeft < 20 ? 2 : 1, 0.1f);
            Lighting.AddLight(Projectile.Center, new Vector3(1, 0.8f, 0.2f) * 0.5f * Projectile.scale);
            base.AI();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        protected Vector4[] data, newData;
        protected int[] dupCount, dupLast;

        public void Vacuum(ISandboxObject sandboxObject)
        {
            float sandIncrease = 0;
            float radius = 0.50f * (70 - Projectile.timeLeft) / 70f;
            float Centrifuge = 0.4f;
            float CleanerRadius = 90.0f;
            float realCleanerRadius = 260.0f * (Projectile.timeLeft / 70f) * MathF.Min(1, (70 - Projectile.timeLeft) * 0.1f);
            CleanerRadius = MathF.Ceiling(realCleanerRadius / 32) * 32;
            radius = 0.5f * realCleanerRadius / CleanerRadius;

            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;

                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                var effectSize = PSSandboxSystem.Instance.effectSize;

                if (data == null || data.Length != effectSize.X * effectSize.Y)
                    data = new Vector4[effectSize.X * effectSize.Y];
                if (newData == null || newData.Length != effectSize.X * effectSize.Y)
                    newData = new Vector4[effectSize.X * effectSize.Y];
                if (dupCount == null || dupCount.Length != effectSize.X * effectSize.Y)
                    dupCount = new int[effectSize.X * effectSize.Y];
                if (dupLast == null || dupLast.Length != effectSize.X * effectSize.Y)
                    dupLast = new int[effectSize.X * effectSize.Y];
                for (int i = 0; i < newData.Length; i++)
                {
                    dupCount[i] = 0;
                    dupLast[i] = -1;
                    newData[i] = Vector4.Zero;
                }

                effectRTSwap.GetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
                for (int j = 0; j < effectSize.Y; j++)
                    for (int i = 0; i < effectSize.X; i++)
                    {
                        Vector2 uv = new Vector2(i / (float)effectSize.X, j / (float)effectSize.Y);
                        Vector4 value = data[i + j * effectSize.X];
                        if (value.W > 0)
                        {
                            float dist = (uv - center).Length();
                            Vector2 targetUV = uv;
                            if (dist <= radius)
                            {
                                targetUV = center + (uv - center).
                                    RotatedBy((1 - dist / radius) * MathF.Cos(dist / radius * 2.5f)) *
                                    MathHelper.Lerp(1.0f, MathF.Pow(dist / radius, 2), 0.1f);
                            }
                            if ((targetUV - center).Length() <= radius * Centrifuge)
                                targetUV = -Vector2.One;
                            Point t = new Point((int)MathF.Round(targetUV.X * effectSize.X), (int)MathF.Round(targetUV.Y * effectSize.Y));
                            int targetIndex = t.X + t.Y * effectSize.X;
                            if (!effectRT.Frame().Contains(t))
                                sandIncrease += 1;
                            else if (data[targetIndex].W == 0)
                            {
                                // 如果目标位置在当前时刻也没有被占用才允许移动，否则目标位置的像素如果移动失败就无处可去
                                // 这里是个随机过程
                                // 等价于：去向同一个目标位置的像素中等概率抽取其中一个成功，其他呆在原位
                                dupCount[targetIndex] += 1;
                                if (Main.rand.NextBool(dupCount[targetIndex]))
                                {
                                    newData[targetIndex] = data[i + j * effectSize.X];
                                    if (dupLast[targetIndex] != -1)
                                        newData[dupLast[targetIndex]] = data[dupLast[targetIndex]];
                                    dupLast[targetIndex] = i + j * effectSize.X;
                                }
                                else newData[i + j * effectSize.X] = data[i + j * effectSize.X];
                            }
                            else
                            {
                                // 移动失败，留在原位
                                newData[i + j * effectSize.X] = data[i + j * effectSize.X];
                            }
                        }
                    }
                effectRT.SetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), newData, 0, newData.Length);
            };

            if (sandboxObject is not HallowCleanerProjectile proj)
                return;
            Player player = Main.player[proj.Projectile.owner];
            // proj.Projectile.Center = player.Bottom + new Vector2(player.Directions.X * 10, -18 - 20) + Vector2.UnitY * player.gfxOffY;
            Vector2 targetPos = proj.Projectile.Center;
            targetPos -= Vector2.One * CleanerRadius;

            // if (proj.Projectile.timeLeft <= 10)
            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback);

            if (player.HeldItem?.ModItem is not VacuumCleanerHallow cleaner)
                return;

            int lastBags = cleaner.sandBagFilled;
            cleaner.sandCount += sandIncrease;
            if (cleaner.sandBagFilled > lastBags)
            {
                var message = "{0} ".FormatWith(cleaner.sandBagFilled) +
                    PixelSandbox.ModTranslate(cleaner.sandBagFilled > 1 ? "SandBagsFilledHint" : "SandBagFilledHint", "Misc.");
                int id = CombatText.NewText(player.getRect(), Color.DarkOrange, message, true);
                SoundEngine.PlaySound(SoundID.Item1 with { MaxInstances = 1, Volume = 0.1f }, player.Center);
            }

        }
    }
}