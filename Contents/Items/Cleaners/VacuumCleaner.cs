using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelSandbox.Contents.Tiles;
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
    public class VacuumCleaner : VacuumCleanerBase
    {
        public override string Texture => (GetType().Namespace + "." + "VacuumCleaner").Replace('.', '/');

        public override float CleanerRadius => 630;

        public override void Vacuum(ISandboxObject sandboxObject, Player player)
        {
            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;
                float radius = 0.45f;
                float centrifuge = 0.07f;
                centrifuge = 10.17f; 

                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                var effectSize = PSSandboxSystem.Instance.effectSize;
                var behaviorShader = PSSandboxSystem.behaviorShader;
                device.SetRenderTarget(effectRT);
                device.Clear(Color.Transparent);
                BlendState blendState = new BlendState();
                blendState.AlphaBlendFunction = BlendFunction.Max;
                blendState.ColorBlendFunction = BlendFunction.Add;
                blendState.AlphaSourceBlend = Blend.One;
                blendState.AlphaDestinationBlend = Blend.One;
                blendState.ColorSourceBlend = Blend.One;
                blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                // Main.spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Contents/Projectiles/LightningRitual", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, Vector2.Zero, null, Color.White, 0, Vector2.Zero, effectRT.Width / 408f, SpriteEffects.None, 0);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                behaviorShader.Parameters["uTex1"].SetValue(effectRTSwap);
                behaviorShader.Parameters["uCircleCenter"].SetValue(center);
                behaviorShader.Parameters["uCircleRadius"].SetValue(radius);
                behaviorShader.Parameters["uCircleCentrifuge"].SetValue(centrifuge);
                behaviorShader.Parameters["uCircleRotation"].SetValue(1.001f * 1.2f * Main.LocalPlayer.direction);
                behaviorShader.CurrentTechnique.Passes["BlackHole"].Apply();
                // behaviorShader.CurrentTechnique.Passes["WhiteHole"].Apply();
                Main.spriteBatch.Draw(effectRTSwap, Vector2.Zero, new Rectangle(0, 0, effectSize.X, effectSize.Y), Color.White);
                Main.spriteBatch.End();
            };
            Vector2 targetPos = player.Bottom + new Vector2(player.Directions.X * 24, -15);
            targetPos -= Vector2.One * CleanerRadius;

            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback);

            var cleaner = sandboxObject as VacuumCleanerBase;
            int lastBags = cleaner.sandBagFilled;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.value = 100000;
            Item.rare = ItemRarityID.Master;
            Item.UseSound = SoundID.Drip with { Pitch = -0.1f };
            sandCount = 0;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SandBag>(), 999);
            recipe.AddRecipeGroup("Sand", 999);
            recipe.AddTile(ModContent.TileType<StrangeStatueTile>());
            recipe.Register();
        }
    }
}