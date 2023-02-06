using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelSandbox.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSSandboxSystem : ModSystem
    {
        public bool inited = false;
        public PSChunk[,] chunks = null;
        public List<PSChunk> recentChunks = null;

        public static Effect behaviorShader;
        public static readonly string behaviorShaderPath = "Effects/behavior";
        public static float frameSeed = 0;

        public static TilePaintSystemV2 tilePaintSystem = null;
        public static TileDrawing tileDrawing;
        public static LegacyLighting lightingEngine;

        public static bool chunkProcessing = false;
        public static bool chunkFullLight = false;
        public int timeTag = 0;

        public RenderTarget2D effectRT = null;
        public RenderTarget2D effectRTSwap = null;


        public static PSSandboxSystem Instance => _instance;
        private static PSSandboxSystem _instance;

        public bool SandEngineAvailable => inited && Main.netMode != NetmodeID.Server && ThreadCheck.IsMainThread && Main.IsGraphicsDeviceAvailable;

        public PSSandboxSystem()
        {
            _instance = this;
        }

        public static PSChunk TryGetChunk(int idx, int idy)
        {
            if (idx < 0 || idx >= Instance.chunks.GetLength(0) || idy < 0 || idy >= Instance.chunks.GetLength(1))
                return null;
            return Instance.chunks[idx, idy];
        }

        public override void PreWorldGen()
        {
            ResetChunks();
            base.PreWorldGen();
        }

        public override void OnWorldLoad()
        {
            ResetChunks();
            base.OnWorldLoad();
        }

        public void ResetChunks()
        {
            inited = false;
            chunks = new PSChunk[(int)MathF.Ceiling(Main.maxTilesX * 16 / (float)PSChunk.CHUNK_WIDTH_INNER), 
                                 (int)MathF.Ceiling(Main.maxTilesY * 16 / (float)PSChunk.CHUNK_HEIGHT_INNER)];
            recentChunks = new List<PSChunk>();
            string chunkFileDir = Path.Combine(Main.WorldPath, "sandbox_{0}".FormatWith(Main.worldName));
            if (!Directory.Exists(chunkFileDir))
                Directory.CreateDirectory(chunkFileDir);
        }

        public override async void OnWorldUnload()
        {
            while (recentChunks.Count > 0)
            {
                await recentChunks[^1].SaveChunk(ChunkFilename(recentChunks[^1]));
                recentChunks.RemoveAt(recentChunks.Count - 1);
            }
            chunks = null;
            inited = false;
            base.OnWorldUnload();
        }

        public override void Load()
        {
            behaviorShader = Mod.Assets.Request<Effect>(behaviorShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += ScreenEffectDecorator;
            On.Terraria.Main.DrawCachedNPCs += DrawHook_DrawCachedNPCs;
            On.Terraria.Lighting.GetColor_int_int += LightColorDecorator;
            On.Terraria.Collision.StepDown += Collision_StepDown;

            tilePaintSystem = Main.instance.TilePaintSystem;
            tileDrawing = new TileDrawing(tilePaintSystem);
            lightingEngine = new LegacyLighting(Main.Camera);
            lightingEngine.Rebuild();

            base.Load();
        }

        private void Collision_StepDown(On.Terraria.Collision.orig_StepDown orig, ref Vector2 position, ref Vector2 velocity, int width, int height, ref float stepSpeed, ref float gfxOffY, int gravDir, bool waterWalk)
        {
            orig(ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, waterWalk);
        }

        private void DrawHook_DrawCachedNPCs(On.Terraria.Main.orig_DrawCachedNPCs orig, Main self, List<int> npcCache, bool behindTiles)
        {
            if (npcCache == Main.instance.DrawCacheNPCsBehindNonSolidTiles)
            {
                Main.spriteBatch.End();
                DrawChunks();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform); ;
            }
            orig(self, npcCache, behindTiles);
        }

        public override void Unload()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= ScreenEffectDecorator;
            On.Terraria.Lighting.GetColor_int_int -= LightColorDecorator;
            On.Terraria.Main.DrawCachedNPCs -= DrawHook_DrawCachedNPCs;
            base.Unload();
        }

        public Color LightColorDecorator(On.Terraria.Lighting.orig_GetColor_int_int orig, int i, int j)
        {
            if (chunkFullLight)
                return Color.White;
            // Hack lighting engine to ensure dark tiles to be drawn
            if (chunkProcessing)
            {
                Color result = orig(i, j);
                if (result.R < 1 && result.G < 1 && result.B < 1)
                {
                    result.R = 1;
                }
                return result;
            }
            else
                return orig(i, j);
        }

        public string ChunkFilename(PSChunk chunk)
        {
            return Path.Combine(Path.Combine(Main.WorldPath, "sandbox_{0}".FormatWith(Main.worldName)), "chunk_tmp_{0}_{1}".FormatWith(chunk.idx, chunk.idy));
        }

        public void EnsureSingleChunk(int i, int j, bool load = true)
        {
            if (i < 0 || i >= Instance.chunks.GetLength(0) || j < 0 || j >= Instance.chunks.GetLength(1))
                return;
            if (chunks[i, j] == null)
                chunks[i, j] = new(i, j);
            chunks[i, j].EnsureRenderTargets();
            if (load && !chunks[i, j].loadingOrLoaded)
                _ = chunks[i, j].LoadChunk(ChunkFilename(chunks[i, j]));
        }

        public void MarkRecent(PSChunk chunk)
        {
            chunk.recentTimeTag = timeTag;
            if (recentChunks.IndexOf(chunk) == -1)
                recentChunks.Add(chunk);
        }


        public void UpdateChunks()
        {
            if (!SandEngineAvailable)
                return;
            var origTargets = Main.graphics.GraphicsDevice.GetRenderTargets();

            timeTag++;
            frameSeed = Main.rand.NextFloat();
            Vector2 topLeft = Main.Camera.ScaledPosition;
            Vector2 bottomRight = Main.Camera.ScaledSize + topLeft;
            int leastChunks = 0;
            var padding = ModContent.GetInstance<SandboxConfig>().ScreenPadding;
            for (int i = (int)((topLeft.X - padding) / PSChunk.CHUNK_WIDTH_INNER); i <= (int)((bottomRight.X + padding) / PSChunk.CHUNK_WIDTH_INNER); i++)
                for (int j = (int)((topLeft.Y - padding) / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)((bottomRight.Y + padding) / PSChunk.CHUNK_HEIGHT_INNER); j++)
                {
                    EnsureSingleChunk(i, j);
                    var chunk = TryGetChunk(i, j);
                    if (chunk != null)
                    {
                        MarkRecent(chunks[i, j]);
                        leastChunks += 1;
                    }
                }
            recentChunks.Sort((PSChunk a, PSChunk b) =>
                a.recentTimeTag == b.recentTimeTag ? 0 : 
                a.recentTimeTag > b.recentTimeTag ? -1 : 1);
            if (recentChunks.Count > leastChunks)
            {
                if (recentChunks.Count > ModContent.GetInstance<SandboxConfig>().ChunkCount || 
                    recentChunks[^1].recentTimeTag + ModContent.GetInstance<SandboxConfig>().UnloadDelay * 60f < timeTag)
                {
                    _ = recentChunks[^1].SaveChunk(ChunkFilename(recentChunks[^1]));
                    recentChunks.RemoveAt(recentChunks.Count - 1);
                }
            }
            PSChunk.StepLightState();
            foreach (var chunk in recentChunks)
                chunk.UpdateCrossChunk();
            foreach (var chunk in recentChunks)
                chunk.Update();
            Main.graphics.GraphicsDevice.SetRenderTargets(origTargets);
        }

        /// <summary>
        /// 从世界坐标采样对应的沙子
        /// soft指不唤醒区块，如果对应区块未加载就返回空
        /// 注意这个操作很慢，不要多用
        /// </summary>
        /// <returns>采样到的沙子数据</returns>
        public Vector4 SoftSampleSand(Vector2 position)
        {
            int idx = (int)(position.X / PSChunk.CHUNK_WIDTH_INNER);
            int idy = (int)(position.Y / PSChunk.CHUNK_HEIGHT_INNER);
            PSChunk chunk = TryGetChunk(idx, idy);
            if (chunk == null || chunk.content == null || chunk.content.IsContentLost)
                return Vector4.Zero;
            Point pos = ((position - chunk.TopLeft + Vector2.One * PSChunk.CHUNK_PADDING) / PSChunk.SAND_SIZE).ToPoint();
            Rectangle frame = PSChunk.SandArea;
            if (frame.Contains(pos))
            {
                var buffer = new Vector4[1];
                chunk.content.GetData(0, new Rectangle(pos.X, pos.Y, 1, 1), buffer, 0, 1);
                return buffer[0];
            }
            return Vector4.Zero;
        }

        /// <summary>
        /// 进行跨区块粒子特效
        /// 会把所有相关区块绘制到一个临时RT上处理
        /// 特效实现使用callback传入
        /// </summary>
        /// <param name="soft"> 不唤醒界外区块 </param>
        public void UpdateChunksEffect(Vector2 topLeft, Vector2 bottomRight, Action<GraphicsDevice, Vector2, Vector2> callback, bool soft = false)
        {
            if (!SandEngineAvailable)
                return;

            topLeft.X = MathF.Floor(topLeft.X / PSChunk.SAND_SIZE) * PSChunk.SAND_SIZE;
            topLeft.Y = MathF.Floor(topLeft.Y / PSChunk.SAND_SIZE) * PSChunk.SAND_SIZE;
            bottomRight.X = MathF.Floor(bottomRight.X / PSChunk.SAND_SIZE) * PSChunk.SAND_SIZE;
            bottomRight.Y = MathF.Floor(bottomRight.Y / PSChunk.SAND_SIZE) * PSChunk.SAND_SIZE;
            Vector2 size = (bottomRight - topLeft) / PSChunk.SAND_SIZE;
            if (size.X <= 1 || size.Y <= 1)
                return;
            EnsureRT(ref effectRT, size.ToPoint());
            EnsureRT(ref effectRTSwap, size.ToPoint());

            var origTargets = Main.graphics.GraphicsDevice.GetRenderTargets();
            var device = Main.graphics.GraphicsDevice;
            if (!soft)
            {
                for (int i = (int)(topLeft.X / PSChunk.CHUNK_WIDTH_INNER); i <= (int)(bottomRight.X / PSChunk.CHUNK_WIDTH_INNER); i++)
                    for (int j = (int)(topLeft.Y / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)(bottomRight.Y / PSChunk.CHUNK_HEIGHT_INNER); j++)
                    {
                        EnsureSingleChunk(i, j);
                        var chunk = TryGetChunk(i, j);
                        if (chunk != null)
                            MarkRecent(chunk);
                    }
            }

            device.SetRenderTarget(effectRTSwap);
            device.Clear(Color.Transparent);
            foreach (var chunk in recentChunks) if (PSChunk.IsChunkReady(chunk))
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                Main.spriteBatch.Draw(chunk.content, (chunk.TopLeft - topLeft) / PSChunk.SAND_SIZE, PSChunk.SandArea, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }

            callback(device, topLeft, bottomRight);

            topLeft /= PSChunk.SAND_SIZE;
            bottomRight /= PSChunk.SAND_SIZE;
            foreach (var chunk in recentChunks) if (PSChunk.IsChunkReady(chunk))
            {
                Vector2 targetPos = chunk.TopLeft / PSChunk.SAND_SIZE;
                float l = MathF.Max(topLeft.X, targetPos.X);
                float r = MathF.Min(bottomRight.X, chunk.BottomRight.X / PSChunk.SAND_SIZE);
                float t = MathF.Max(topLeft.Y, targetPos.Y);
                float b = MathF.Min(bottomRight.Y, chunk.BottomRight.Y / PSChunk.SAND_SIZE);
                Vector2 offset = new Vector2(MathF.Max(l - targetPos.X, 0), MathF.Max(t - targetPos.Y, 0));
                Rectangle frame = new Rectangle((int)offset.X, (int)offset.Y, (int)(r - l), (int)(b - t));
                if (frame.Width <= 0 || frame.Height <= 0)
                    continue;
                Vector2 fracOffset = (chunk.BottomRight / PSChunk.SAND_SIZE - targetPos) - frame.BottomRight();
                if (r >= bottomRight.X) fracOffset.X = 0;
                if (b >= bottomRight.Y) fracOffset.Y = 0;
                device.SetRenderTarget(chunk.content);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                frame.Offset((new Vector2(l, t) - topLeft + fracOffset - offset).ToPoint());
                Main.spriteBatch.Draw(effectRT, offset + PSChunk.SandArea.TopLeft(), frame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }

            Main.graphics.GraphicsDevice.SetRenderTargets(origTargets);
        }

        public void DrawChunks()
        {
            if (!SandEngineAvailable)
                return;
            var origTargets = Main.graphics.GraphicsDevice.GetRenderTargets();

            Vector2 topLeft = Main.Camera.ScaledPosition;
            Vector2 bottomRight = Main.Camera.ScaledSize + topLeft;
            for (int i = (int)(topLeft.X / PSChunk.CHUNK_WIDTH_INNER); i <= (int)(bottomRight.X / PSChunk.CHUNK_WIDTH_INNER); i++)
                for (int j = (int)(topLeft.Y / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)(bottomRight.Y / PSChunk.CHUNK_HEIGHT_INNER); j++)
                {
                    EnsureSingleChunk(i, j);
                    TryGetChunk(i, j)?.Draw();
                }

            Main.graphics.GraphicsDevice.SetRenderTargets(origTargets);
        }

        /// <summary>
        /// 进行游戏内容相关更新
        /// </summary>
        public void UpdateGameContents()
        {
            if (!SandEngineAvailable)
                return;
            var origTargets = Main.graphics.GraphicsDevice.GetRenderTargets();

            foreach (var player in Main.player) if (player.active)
            {
                if (!(player.active && (player.HeldItem?.ModItem?.GetType().IsAssignableTo(typeof(PSModItem)) ?? false)))
                    continue;
                if (player.itemAnimation <= 0 || !player.ItemAnimationActive)
                    continue;
                PSModItem item = player.HeldItem.ModItem as PSModItem;
                var nl = null as PSModItem;
                item?.Behavior.PlayerUseAnimation?.Invoke(item, player);
            }

            foreach (var p in Main.projectile) if (p.active)
            {
                PSModProjectile projectile = p.ModProjectile as PSModProjectile;
                projectile?.Behavior.Update?.Invoke(projectile);
            }
            Main.graphics.GraphicsDevice.SetRenderTargets(origTargets);
        }

        public static void EnsureRT(ref RenderTarget2D content, Point size)
        {
            if (content == null || content.Width != size.X || content.Height != size.Y || content.IsContentLost)
            {
                if (content != null && !content.IsDisposed)
                    content.Dispose();
                content = new RenderTarget2D(Main.graphics.GraphicsDevice,
                    size.X, size.Y,
                    false, PSChunk.CONTENT_FORMAT, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            }
        }

        public override void PostUpdateDusts()
        {
            UpdateChunks();
            UpdateGameContents();
            base.PostUpdateDusts();
        }

        public override void PostUpdateEverything()
        {
            inited = true;
            tileDrawing.Update();
            base.PostUpdateEverything();
        }

        public void ScreenEffectDecorator(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig,
                                          Terraria.Graphics.Effects.FilterManager self,
                                          RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
                                          RenderTarget2D screenTarget2, Color clearColor)
        {
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

    }

}