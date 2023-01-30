using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSChunk
    {
        public static readonly int SAND_SIZE = 2;
        public static readonly int CHUNK_WIDTH_SAND_INNER = 512;
        public static readonly int CHUNK_HEIGHT_SAND_INNER = 512;
        public static readonly int CHUNK_WIDTH_LIGHT = 32;
        public static readonly int CHUNK_HEIGHT_LIGHT = 32;
        public static readonly int PADDING_SAND = 2;
        public static int MASK_UPDATE_INTERVAL = 10;
        public static readonly SurfaceFormat CONTENT_FORMAT = SurfaceFormat.Vector4;

        public static int CHUNK_WIDTH_SAND => CHUNK_HEIGHT_SAND_INNER + 2 * PADDING_SAND;
        public static int CHUNK_HEIGHT_SAND => CHUNK_HEIGHT_SAND_INNER + 2 * PADDING_SAND;
        public static int CHUNK_WIDTH => CHUNK_WIDTH_SAND * SAND_SIZE;
        public static int CHUNK_HEIGHT => CHUNK_HEIGHT_SAND * SAND_SIZE;
        public static int CHUNK_WIDTH_INNER => CHUNK_WIDTH_SAND_INNER * SAND_SIZE;
        public static int CHUNK_HEIGHT_INNER => CHUNK_HEIGHT_SAND_INNER * SAND_SIZE;
        public static int CHUNK_PADDING => PADDING_SAND * SAND_SIZE;

        public int idx;
        public int idy;
        public RenderTarget2D content = null;
        public RenderTarget2D swap = null;

        public RenderTarget2D mask = null;
        public RenderTarget2D light = null;
        private int maskUpdateCounter = 0;
        public int recentTimeTag = 0;
        public bool loading = false;

        private Vector4[] buffer = null;

        public Vector2 Position => new Vector2(idx * CHUNK_WIDTH_INNER, idy * CHUNK_HEIGHT_INNER);
        public Vector2 TopLeft => Position;
        public Vector2 BottomRight => new Vector2((idx + 1) * CHUNK_WIDTH_INNER, (idy + 1) * CHUNK_HEIGHT_INNER);

        public PSChunk(int idx, int idy)
        {
            this.idx = idx;
            this.idy = idy;
        }

        public void UpdateMaskAndLight()
        {
            SampleLight();
            PSSandboxSystem.chunkProcessing = true;
            var origDrawToScreen = Main.drawToScreen;
            var origViewMatrix = Main.GameViewMatrix;
            var origScreenPosition = Main.screenPosition;
            var origScreenWidth = Main.screenWidth;
            var origScreenHeight = Main.screenHeight;
            var origOffscreenRange = Main.offScreenRange;
            var origSampleState = Main.DefaultSamplerState.Filter;

            var origLightingMode = Lighting.Mode;
            var origRenderCount = Main.renderCount;
            var origMapDelay = Main.mapDelay;
            var origMapTime = Main.mapTime;
            var origScreenLastPosition = Main.screenLastPosition;

            FieldInfo _activeEngineInfo = typeof(Lighting).GetField("_activeEngine", BindingFlags.Static | BindingFlags.NonPublic);
            ILightingEngine origActiveEngine = (ILightingEngine)_activeEngineInfo.GetValue(null);

            GraphicsDevice graphicDevice = Main.graphics.GraphicsDevice;
            graphicDevice.SetRenderTarget(mask);
            graphicDevice.Clear(Color.Transparent);

            var currentEngine = PSSandboxSystem.lightingEngine;

            _activeEngineInfo.SetValue(null, currentEngine);
            Main.mapDelay = 99;
            Main.mapTime = 99;

            Main.drawToScreen = true;
            Main.offScreenRange = 0;

            Main.screenWidth = CHUNK_HEIGHT + 2 * CHUNK_PADDING;
            Main.screenHeight = CHUNK_HEIGHT + 2 * CHUNK_PADDING;

            Main.screenPosition = TopLeft - Vector2.One * CHUNK_PADDING;

            Main.GameViewMatrix = new Terraria.Graphics.SpriteViewMatrix(Main.graphics.GraphicsDevice);

            // Tiles part 1
            var tileDrawing = PSSandboxSystem.tileDrawing;
            tileDrawing.PreDrawTiles(false, true, true);

            // Tiles part 2
            tileDrawing.PreDrawTiles(true, true, false);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None,
                Main.Rasterizer, null, Matrix.Identity);
            tileDrawing.Draw(true, false /* Unused */, true, -1);
            Main.spriteBatch.End();

            Main.drawToScreen = origDrawToScreen;
            Main.screenPosition = origScreenPosition;
            Main.screenWidth = origScreenWidth;
            Main.screenHeight = origScreenHeight;
            Main.offScreenRange = origOffscreenRange;
            Main.DefaultSamplerState.Filter = origSampleState;

            Lighting.Mode = origLightingMode;
            Main.renderCount = origRenderCount;
            _activeEngineInfo.SetValue(null, origActiveEngine);

            Main.mapDelay = origMapDelay;
            Main.mapTime = origMapTime;
            Main.screenLastPosition = origScreenLastPosition;

            Main.GameViewMatrix = origViewMatrix;

            PSSandboxSystem.chunkProcessing = false;
        }

        public void SampleLight()
        {
            Color[] buffer = new Color[CHUNK_WIDTH_LIGHT * CHUNK_HEIGHT_LIGHT];
            for (int i = 0; i < CHUNK_WIDTH_LIGHT; i++)
                for (int j = 0; j < CHUNK_HEIGHT_LIGHT; j++)
                {
                    buffer[i + j * CHUNK_WIDTH_LIGHT] = Lighting.GetColor(((TopLeft - Vector2.One * CHUNK_PADDING +
                        new Vector2(i * CHUNK_WIDTH / (float)CHUNK_WIDTH_LIGHT,
                                    j * CHUNK_HEIGHT / (float)CHUNK_HEIGHT_LIGHT)) / 16).ToPoint());
                }
            light.SetData(buffer);
        }

        public void Update()
        {
            if (mask.IsContentLost || --maskUpdateCounter <= 0)
            {
                UpdateMaskAndLight();
                maskUpdateCounter = MASK_UPDATE_INTERVAL;
            }
            Utils.Swap(ref content, ref swap);
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            
            device.SetRenderTarget(swap);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            PSChunk other = null;
            float[] targetGridX = new float[] { 0, PADDING_SAND, PADDING_SAND + CHUNK_WIDTH_SAND_INNER };
            float[] targetGridY = new float[] { 0, PADDING_SAND, PADDING_SAND + CHUNK_HEIGHT_SAND_INNER };
            float[] sourceGridX = new float[] { PADDING_SAND, PADDING_SAND, CHUNK_WIDTH_SAND_INNER };
            float[] sourceGridY = new float[] { PADDING_SAND, PADDING_SAND, CHUNK_HEIGHT_SAND_INNER };
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    if (i != 0 || j != 0)
                    {
                        other = PSSandboxSystem.TryGetChunk(idx + i, idy + j);
                        if (other != null && other.recentTimeTag >= recentTimeTag - 1)
                        {
                            Main.spriteBatch.Draw(i < 0 || (i == 0 && j < 0) ? other.swap : other.content,
                                new Vector2(targetGridX[i + 1], targetGridY[j + 1]),
                                new Rectangle((int)sourceGridX[1 - i], (int)sourceGridY[1 - j],
                                    i == 0 ? CHUNK_WIDTH_SAND_INNER : PADDING_SAND,
                                    j == 0 ? CHUNK_HEIGHT_SAND_INNER : PADDING_SAND),
                                Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                        }
                        else
                        {
                            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                                new Vector2(targetGridX[i + 1], targetGridY[j + 1]),
                                new Rectangle((int)sourceGridX[1 - i], (int)sourceGridY[1 - j],
                                    i == 0 ? CHUNK_WIDTH_SAND_INNER : PADDING_SAND,
                                    j == 0 ? CHUNK_HEIGHT_SAND_INNER : PADDING_SAND),
                                Color.Transparent, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                        }
                    }
            Main.spriteBatch.End();

            device.SetRenderTarget(content);
            device.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            Effect behaviorShader = PSSandboxSystem.behaviorShader;
            behaviorShader.Parameters["uTex1"].SetValue(swap);
            behaviorShader.Parameters["uTex2"].SetValue(mask);
            behaviorShader.Parameters["uStep"].SetValue(new Vector2((PSSandboxSystem.frameSeed > 0.5f ? -1 : 1) / (float)CHUNK_WIDTH_SAND,
                                                                                                              1 / (float)CHUNK_HEIGHT_SAND));
            behaviorShader.CurrentTechnique.Passes["Compute"].Apply();
            Main.spriteBatch.Draw(swap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();


            if (Main.LocalPlayer.controlThrow)
            {
                Vector2 mousePos = new Vector2(Main.mouseX, Main.mouseY) + Main.Camera.UnscaledPosition;
                Point pos = ((mousePos - TopLeft) / SAND_SIZE).ToPoint();
                Rectangle frame = content.Frame();
                frame.Inflate(-1, -1);
                if (frame.Contains(pos))
                {
                    Vector4[] buffer = new Vector4[4];
                    for (int i = 0; i < 4; i++)
                    {
                        buffer[i] = new Vector4(Color.SandyBrown.ToVector3() * 0.5f * (1 + Main.rand.NextFloat()), Main.rand.NextFloat());
                    }
                    content.SetData(0, new Rectangle(pos.X, pos.Y, 2, 2), buffer, 0, buffer.Length);
                }
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                // Main.spriteBatch.Draw(TextureAssets.Bubble.Value, pos.ToVector2(), Color.White);
                Main.spriteBatch.End();
            }
        }

        public void Draw()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            Effect behaviorShader = PSSandboxSystem.behaviorShader;
            behaviorShader.Parameters["uTex1"].SetValue(content);
            behaviorShader.Parameters["uTex3"].SetValue(light);
            behaviorShader.CurrentTechnique.Passes["Display"].Apply();
            // Main.spriteBatch.Draw(mask, Position - Main.screenPosition - Vector2.One * CHUNK_PADDING, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Rectangle frame = content.Frame();
            frame.Inflate(-PADDING_SAND, -PADDING_SAND);
            Main.spriteBatch.Draw(content, Position - Main.screenPosition, frame, Color.White, 0, Vector2.Zero, SAND_SIZE, SpriteEffects.None, 0);
            // Main.spriteBatch.Draw(content, Position - Main.screenPosition - Vector2.One * CHUNK_PADDING, null, Color.White, 0, Vector2.Zero, SAND_SIZE, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }

        public async Task SaveChunk(string filename)
        {
            FileStream fileStream = null;
            EnsureRenderTargets();
            EnsureBuffer();
            try
            {
                using (fileStream = File.Open(filename, FileMode.OpenOrCreate))
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    content.GetData(buffer);
                    List<byte> bytesArray = new();
                    for (int i = 0; i < CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND; i++)
                    {
                        bytesArray.AddRange(BitConverter.GetBytes(buffer[i].X));
                        bytesArray.AddRange(BitConverter.GetBytes(buffer[i].Y));
                        bytesArray.AddRange(BitConverter.GetBytes(buffer[i].Z));
                        bytesArray.AddRange(BitConverter.GetBytes(buffer[i].W));
                    }
                    await fileStream.WriteAsync(bytesArray.ToArray());
                }
            }
            catch (Exception ex)
            {
            }
        }

        public async Task LoadChunk(string filename)
        {
            EnsureRenderTargets();
            EnsureBuffer();
            if (!File.Exists(filename))
                return;
            if (loading)
                return;
            loading = true;
            FileStream fileStream = null;
            try
            {
                using (fileStream = File.Open(filename, FileMode.Open))
                {
                    fileStream.Seek(0, SeekOrigin.Begin);
                    byte[] bytesArray = new byte[CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND * 16];
                    await fileStream.ReadAsync(bytesArray, 0, CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND * 16);
                    for (int i = 0; i < CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND; i++)
                        buffer[i] = new Vector4
                        (
                            BitConverter.ToSingle(bytesArray.AsSpan(i * 16 + 0, 4)),
                            BitConverter.ToSingle(bytesArray.AsSpan(i * 16 + 4, 4)),
                            BitConverter.ToSingle(bytesArray.AsSpan(i * 16 + 8, 4)),
                            BitConverter.ToSingle(bytesArray.AsSpan(i * 16 + 12, 4))
                        );
                    Main.QueueMainThreadAction(() => { try { content.SetData(buffer); } catch { } });
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void EnsureBuffer()
        {
            if (buffer == null || buffer.Length != CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND)
                buffer = new Vector4[CHUNK_WIDTH_SAND * CHUNK_HEIGHT_SAND];
        }

        public void EnsureRenderTargets()
        {
            EnsureContent(ref content);
            EnsureContent(ref swap);
            EnsureMask();
            EnsureLight();
        }

        public void EnsureContent(ref RenderTarget2D content)
        {
            if (content == null || content.Width != CHUNK_WIDTH_SAND || content.Height != CHUNK_HEIGHT_SAND || content.IsContentLost)
            {
                if (content != null && !content.IsDisposed)
                    content.Dispose();
                content = new RenderTarget2D(Main.graphics.GraphicsDevice,
                    CHUNK_WIDTH_SAND, CHUNK_HEIGHT_SAND,
                    false, CONTENT_FORMAT, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        public void EnsureMask()
        {
            if (mask == null || mask.Width != CHUNK_WIDTH || mask.Height != CHUNK_HEIGHT || mask.IsContentLost)
            {
                if (mask != null && !mask.IsDisposed)
                    mask.Dispose();
                mask = new RenderTarget2D(Main.graphics.GraphicsDevice,
                    CHUNK_WIDTH, CHUNK_HEIGHT,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }
        public void EnsureLight()
        {
            if (light == null || light.Width != CHUNK_WIDTH_LIGHT || light.Height != CHUNK_HEIGHT_LIGHT || light.IsContentLost)
            {
                if (light != null && !light.IsDisposed)
                    light.Dispose();
                light = new RenderTarget2D(Main.graphics.GraphicsDevice,
                    CHUNK_WIDTH_LIGHT, CHUNK_HEIGHT_LIGHT,
                    false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
        }

        ~PSChunk()
        {
            Main.QueueMainThreadAction(() =>
            {
                if (content != null && !content.IsDisposed)
                    content.Dispose();
                if (swap != null && !swap.IsDisposed)
                    swap.Dispose();
                if (mask != null && !mask.IsDisposed)
                    mask.Dispose();
            });
        }
    }
}