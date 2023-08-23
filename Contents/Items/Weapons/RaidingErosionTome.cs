using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Weapons
{
    public class RaidingErosionTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 10));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 1;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.holdStyle = ItemHoldStyleID.None;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.mana = 14;
            Item.shoot = ModContent.ProjectileType<RaidingErosionTomeProjectile>();
            // Item.UseSound = SoundID.Item70 with { Pitch = -0.5f };
            Item.UseSound = SoundID.Item73 with { Pitch = -0.5f };
            // Item.shoot = ProjectileID.DeerclopsRangedProjectile;
            // Item.shoot = ModContent.ProjectileType<PebbleProjectile>();
            Item.shootSpeed = 8;
        }
    }

    public class RaidingErosionTomeProjectile : PSModProjectile
    {
        // public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WhiteTigerPounce}";
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.DeerclopsRangedProjectile}";
        // public override string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public static PSVectorField vectorField;


        public RaidingErosionTomeProjectile() => Behavior.Update = Vacuum;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.WaterBolt);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 170;
            Projectile.width = Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.netImportant = true;
            Projectile.damage = 1;
            DrawOriginOffsetX = 0;
            DrawOriginOffsetY = 0;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Main.rand.NextFloatDirection();
            Projectile.scale = 0.1f;
            var slot = SoundEngine.PlaySound(SoundID.DD2_BookStaffTwisterLoop with { PitchVariance = 0.5f }, Projectile.Center);
            Projectile.localAI[1] = slot.ToFloat();
            Projectile.ai[0] = -1;
            //Projectile.velocity = Projectile.velocity.RotatedBy(-MathF.PI * 0.5f);
            base.OnSpawn(source);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            return false;
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
            for (int i = 1; i < pos.Count * 0; i++)
            {
                Main.spriteBatch.Draw(asset,
                    pos[i] - Main.screenPosition + Projectile.Size * 0.5f, null,
                    new Color(1.0f, 0.8f, 0.4f) * ((255f - Projectile.alpha) / 255f) * 0.8f * MathHelper.Lerp(0, 1, i / (float)pos.Count),
                    (pos[i - 1] - pos[i]).ToRotation() + 0.5f * MathF.PI,
                    asset.Size() * 0.5f, 1.0f, SpriteEffects.None, 0);
            }
            if (Projectile.ai[0] >= 0)
            {
                /*
                NPC npc = Main.npc[(int)Projectile.ai[0]];
                if (npc.active)
                {
                    if (Projectile.timeLeft > 8)
                    Dust.QuickDustLine(Projectile.Center, npc.Center, 10, Color.Red);
                    if (Projectile.timeLeft < 2)
                    Dust.QuickDustLine(Projectile.Center, npc.Center, 100, Color.White);
                }
                */
            }

            float realCleanerRadius = 260.0f * (Projectile.timeLeft / 70f) * MathF.Min(1, (70 - Projectile.timeLeft) * 0.1f);
            asset = TextureAssets.Extra[174].Value;
            Main.spriteBatch.Draw(asset,
                pos[^1] - Main.screenPosition + Projectile.Size * 0.5f, null,
                new Color(1.0f, 0.8f, 0.4f) * ((255f - Projectile.alpha) / 255f) * 0.8f * 1, 0, asset.Size() * 0.5f,
                Projectile.localAI[0] * 2 * 0.4f / asset.Width, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
			// miscShaderData.Apply(null);
            GameShaders.Misc.Values.Skip(9).FirstOrDefault().Apply(null);

            Main.graphics.GraphicsDevice.Textures[0] = TextureAssets.Extra[156].Value;
            Main.graphics.GraphicsDevice.Textures[0] = TextureAssets.Extra[192].Value;
            VertexStrip strip = new VertexStrip();
            var rotations = oldPos.Zip(oldPos.Skip(1), (a, b) => a - b).Select((a) => a.ToRotation());
            strip.PrepareStrip(oldPos, rotations.Prepend(rotations.FirstOrDefault()).ToArray(),
                (x) => Color.White, (x) => MathHelper.Lerp(0, 100 + 0 * MathF.Sin(x * 10), MathF.Min(1, 2 * x)), -Main.screenPosition + Projectile.Size * 0.5f);
            strip.DrawTrail();


            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            base.PostDraw(lightColor);
        }

        public override void AI()
        {
            // Projectile.velocity.Y -= 0.1f;
            // Projectile.Center = Main.MouseWorld;
            SoundEngine.TryGetActiveSound(SlotId.FromFloat(Projectile.localAI[1]), out var activeSound);
            if (activeSound != null)
            {
                if (Projectile.timeLeft <= 10)
                {
                    activeSound.Volume = 0f;
                    activeSound.Stop();
                }
                else
                {
                    activeSound.Volume = Projectile.timeLeft / 170f;
                    activeSound.Position = Projectile.Center;
                }
            }
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0, 0, 0, default, 2);
                dust.velocity = (dust.position - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.5f;
            }
            if (Projectile.timeLeft <= 10)
            {
                Projectile.localAI[0] = MathHelper.Lerp(Projectile.localAI[0], Projectile.ai[1], 0.5f);
                if (Projectile.ai[0] == -1)
                {
                    NPC npc = Projectile.FindTargetWithinRange(800, true);
                    if (npc != null)
                    {
                        Projectile.ai[0] = npc.whoAmI;
                        ParticleOrchestraSettings settings = new ParticleOrchestraSettings { PositionInWorld = Projectile.Center };
                        ParticleOrchestrator.RequestParticleSpawn(true, ParticleOrchestraType.Keybrand, settings, new int?(Projectile.owner));
                    }
                    else
                        Projectile.ai[0] = -2;

                    if (Projectile.owner == Main.myPlayer)
                    {
                    }
                    if (Projectile.ai[1] > 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item70 with { Pitch = 1.0f }, Projectile.Center);
                        SoundEngine.PlaySound(SoundID.Item105 with { Pitch = 0.0f, Volume = 0.5f }, Projectile.Center);
                    }
                    else
                    {
                        SoundEngine.PlaySound(SoundID.Item51 with { Volume = 0.5f, Pitch = -0.5f }, Projectile.Center);
                    }
                }
            }
            if (Projectile.timeLeft <= 20)
            { 
                if (Projectile.ai[1] <= 0 && Main.rand.NextBool(6))
                {
                    Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(), Projectile.Center + Main.rand.NextVector2Circular(20, 20), 
                        Vector2.Zero, GoreID.Pages);
                }
            }
            // Projectile.rotation += 5e-5f * MathF.Pow(Projectile.timeLeft, 2);
            // Projectile.velocity = Projectile.velocity.RotatedBy(0.05f);
            Projectile.velocity *= Projectile.timeLeft <= 30 ? 0.9f : 0.99f;
            Projectile.alpha = Math.Max(0, 255 - 10 * Projectile.timeLeft);
            // Projectile.scale = MathHelper.Lerp(Projectile.scale, Projectile.timeLeft < 20 ? 2 : 1, 0.1f);
            Lighting.AddLight(Projectile.Center, new Vector3(1, 0.8f, 0.2f) * 0.5f * Projectile.scale);
            base.AI();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = 0;
            else
                Projectile.velocity.Y = 0;
            Projectile.velocity = Projectile.velocity * 2 - oldVelocity;
            return false;
        }

        protected Vector4[] data, newData;
        protected int[] dupCount, dupLast;

        public void Vacuum(ISandboxObject sandboxObject)
        {
            float sandIncrease = 0;
            float radius = 0.40f * (70 - Projectile.timeLeft) / 70f;
            float Centrifuge = 0.0f;
            float CleanerRadius = 90.0f;
            float realCleanerRadius = 260.0f * MathF.Min(1, Projectile.timeLeft / 70f) * MathF.Min(1, (170 - Projectile.timeLeft) * 0.1f);
            CleanerRadius = MathF.Ceiling(realCleanerRadius / 32) * 32;
            CleanerRadius = 220;
            radius = 0.5f * realCleanerRadius / CleanerRadius;

            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;

                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                var effectSize = PSSandboxSystem.Instance.effectSize;

                var behaviorShader = PSSandboxSystem.behaviorShader;
                device.SetRenderTarget(effectRT);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                behaviorShader.Parameters["uTex0"].SetValue(effectRTSwap);
                var windLeft = (Vector2 uv) =>
                {
                    Vector2 r = uv - Vector2.One * 0.5f;
                    if (r.Length() >= 0.5f)
                        return uv;
                    // return Vector2.One * 0.5f + r.RotatedBy(r.Length() * 0.5f) * 1.0f;
                    var dist = r.Length();
                    return Vector2.One * 0.5f + r.
                                    RotatedBy(-0.2f * (1 - dist / 0.5f) * MathF.Cos(dist / 0.5f * 2.5f)) *
                                    MathHelper.Lerp(1.0f, MathF.Pow(dist / 0.5f, 2), 0.1f);
                };
                var windRight = (Vector2 uv) =>
                {
                    Vector2 r = uv - Vector2.One * 0.5f;
                    if (r.Length() >= 0.5f)
                        return uv;
                    // return Vector2.One * 0.5f + r.RotatedBy(r.Length() * 0.5f) * 1.0f;
                    var dist = r.Length();
                    return Vector2.One * 0.5f + r.
                                    RotatedBy(0.2f * (1 - dist / 0.5f) * MathF.Cos(dist / 0.5f * 2.5f)) *
                                    MathHelper.Lerp(1.0f, MathF.Pow(dist / 0.5f, 2), 0.1f);
                };
                var vectorField = PSVectorField.VectorField(110, 110, Projectile.direction > 0 ? windRight : windLeft, 5);
                var field = vectorField.GetTexture();
                behaviorShader.Parameters["uTex1"].SetValue(field.Item1);
                behaviorShader.Parameters["uTex2"].SetValue(field.Item2);
                behaviorShader.Parameters["uUVScale"].SetValue(effectSize.ToVector2() / effectRT.Size());
                behaviorShader.CurrentTechnique.Passes["VectorField"].Apply();
                Main.spriteBatch.Draw(effectRTSwap, Vector2.Zero, new Rectangle(0, 0, effectSize.X, effectSize.Y), Color.White);
                Main.spriteBatch.End();
                if (Projectile.ai[1] == 0)
                {
                    Vector4[] centerData = new Vector4[100];
                    Point sample = (Main.rand.NextVector2Circular((effectSize.X - 10) * 0.3f, (effectSize.Y - 10) * 0.3f) +
                        (effectSize.ToVector2() - Vector2.One * 10) * 0.5f).ToPoint();
                    effectRT.GetData(0, new Rectangle(sample.X, sample.Y, 10, 10), centerData, 0, 100);
                    sandIncrease = 0;
                    for (int i = 0; i < centerData.Length; i++)
                        if (centerData[i].W > 0) sandIncrease += 1;
                    sandIncrease = Math.Min(sandIncrease, centerData.Length / 3 - sandIncrease);
                }
            };

            if (sandboxObject is not RaidingErosionTomeProjectile proj)
                return;
            Player player = Main.player[proj.Projectile.owner];
            // proj.Projectile.Center = player.Bottom + new Vector2(player.Directions.X * 10, -18 - 20) + Vector2.UnitY * player.gfxOffY;
            Vector2 targetPos = proj.Projectile.Center;
            targetPos -= Vector2.One * CleanerRadius;

            sandIncrease = 0;
            // if (proj.Projectile.timeLeft <= 10)
            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback, true);
            CleanerRadius = 110;
            if (sandIncrease > 2)
            {
                // Projectile.ai[1] = MathF.Min(1000, Projectile.ai[1] + sandIncrease);
                Projectile.ai[1] = 1000;
            }
            targetPos = proj.Projectile.Center - Vector2.One * CleanerRadius;
            targetPos.Y -= 150;
            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback, true);
            targetPos.Y += 300;
            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback, true);
        }
    }
}