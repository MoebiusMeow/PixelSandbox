using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSGlobalTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            int idx = (int)(i * 16 / PSChunk.CHUNK_WIDTH_INNER);
            int idy = (int)(j * 16 / PSChunk.CHUNK_HEIGHT_INNER);
            PSChunk chunk = PSSandboxSystem.TryGetChunk(idx, idy);
            if (!fail && chunk != null && !chunk.processing)
            {
                bool solid;
                if (TileID.Sets.DrawTileInSolidLayer[type].HasValue)
                    solid = TileID.Sets.DrawTileInSolidLayer[type].Value;
                else
                    solid = Main.tileSolid[type];

                if (ThreadCheck.IsMainThread)
                {
                    var origTargets = Main.graphics.GraphicsDevice.GetRenderTargets();
                    PSSandboxSystem.Instance.EnsureSingleChunk(idx, idy, false);
                    PSSandboxSystem.Instance.MarkRecent(chunk);

                    Vector2 startPosition = new Vector2(i, j) * 16 - chunk.TopLeft + Vector2.One * PSChunk.CHUNK_PADDING;
                    PSSandboxSystem.chunkFullLight = true;
                    // Main.graphics.GraphicsDevice.SetRenderTarget(chunk.nonSolidMask);
                    // Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                    // Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, startPosition, new Rectangle(0, 0, 16, 16), Color.Transparent, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                    // Main.spriteBatch.End();
                    // chunk.DrawWorld(new Vector2(i, j) * 16, Vector2.One * 16, !solid);
                    // chunk.DrawWorld(chunk.TopLeft - Vector2.One * PSChunk.CHUNK_PADDING, new Vector2(PSChunk.CHUNK_WIDTH, PSChunk.CHUNK_HEIGHT), solid, false);
                    PSSandboxSystem.chunkFullLight = false;

                    Main.graphics.GraphicsDevice.SetRenderTarget(chunk.content);
                    BlendState blendState = new BlendState();
                    blendState.AlphaBlendFunction = BlendFunction.Max;
                    blendState.ColorBlendFunction = BlendFunction.Add;
                    blendState.AlphaSourceBlend = Blend.One;
                    blendState.AlphaDestinationBlend = Blend.One;
                    blendState.ColorSourceBlend = Blend.One;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState);
                    Main.spriteBatch.Draw(chunk.nonSolidMask, startPosition / PSChunk.SAND_SIZE,
                        new Rectangle((int)startPosition.X + 4, (int)startPosition.Y + 4, 8, 8), Color.White, (Main.rand.NextFloat() + 0.5f) * MathF.PI, Vector2.One * 8, 0.7f,
                        SpriteEffects.None, 0);
                    Main.spriteBatch.End();

                    Main.graphics.GraphicsDevice.SetRenderTarget(chunk.mask);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, startPosition, new Rectangle(0, 0, 16, 16), Color.Transparent, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                    Main.graphics.GraphicsDevice.SetRenderTargets(origTargets);
                }
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
    }
}