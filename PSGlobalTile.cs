using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSGlobalTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            int idx = (int)(i * 16 / PSChunk.CHUNK_WIDTH_INNER);
            int idy = (int)(j * 16 / PSChunk.CHUNK_HEIGHT_INNER);
            PSSandboxSystem.Instance.EnsureSingleChunk(idx, idy);
            PSChunk chunk = PSSandboxSystem.Instance.chunks[idx, idy];
            PSSandboxSystem.Instance.MarkRecent(chunk);
            if (!fail)
            {
                var texture = Main.instance.TilesRenderer.GetTileDrawTexture(Main.tile[i, j], i, j);
                Main.QueueMainThreadAction(() =>
                {
                    TileDrawInfo info = new TileDrawInfo();
                    info.tileCache = Main.tile[i, j];
                    info.typeCache = (ushort)type;
                    Main.graphics.GraphicsDevice.SetRenderTarget(chunk.content);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    Main.spriteBatch.Draw(texture, (new Vector2(i, j) * 16 - chunk.TopLeft - Vector2.One * 8) / PSChunk.SAND_SIZE,
                        new Rectangle(0, 0, 16, 16), Color.White, 0, Vector2.Zero, 1.0f,
                        SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                });
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
    }
}