using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public class PSSandboxSystem : ModSystem
    {
        public static int PREFERED_CHUNKS = 12;

        public bool inited = false;
        public PSChunk[,] chunks = null;
        public List<PSChunk> recentChunks = null;

        public static Effect behaviorShader;
        public static readonly string behaviorShaderPath = "Effects/behavior";
        public static float frameSeed = 0;

        public static TilePaintSystemV2 tilePaintSystem = null;
        public static TileDrawing tileDrawing;
        public static LightingEngine lightingEngine;

        public static bool chunkProcessing = false;
        public int timeTag = 0;

        public static PSSandboxSystem Instance => _instance;
        private static PSSandboxSystem _instance;

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

        public override void OnWorldLoad()
        {
            inited = false;
            chunks = new PSChunk[(int)MathF.Ceiling(Main.maxTilesX * 16 / (float)PSChunk.CHUNK_WIDTH_INNER), 
                                 (int)MathF.Ceiling(Main.maxTilesY * 16 / (float)PSChunk.CHUNK_HEIGHT_INNER)];
            recentChunks = new List<PSChunk>();
            string chunkFileDir = Path.Combine(Main.WorldPath, "sandbox_{0}".FormatWith(Main.worldName));
            if (!Directory.Exists(chunkFileDir))
                Directory.CreateDirectory(chunkFileDir);
            base.OnWorldLoad();
        }

        public override void OnWorldUnload()
        {
            Main.QueueMainThreadAction(() =>
            {
                while (recentChunks.Count > 0)
                {
                    _ = recentChunks[^1].SaveChunk(ChunkFilename(recentChunks[^1]));
                    recentChunks.RemoveAt(recentChunks.Count - 1);
                }
                chunks = null;
            });
            base.OnWorldUnload();
        }

        public override void Load()
        {
            behaviorShader = Mod.Assets.Request<Effect>(behaviorShaderPath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            On.Terraria.Graphics.Effects.FilterManager.EndCapture += ScreenEffectDecorator;
            On.Terraria.GameContent.Drawing.TileDrawing.PostDrawTiles += DrawHook_TileEntities;
            On.Terraria.Lighting.GetColor_int_int += LightColorDecorator;

            tilePaintSystem = Main.instance.TilePaintSystem;
            tileDrawing = new TileDrawing(tilePaintSystem);
            lightingEngine = new LightingEngine();
            lightingEngine.Rebuild();

            base.Load();
        }


        public override void Unload()
        {
            On.Terraria.Graphics.Effects.FilterManager.EndCapture -= ScreenEffectDecorator;
            On.Terraria.GameContent.Drawing.TileDrawing.PostDrawTiles -= DrawHook_TileEntities;
            On.Terraria.Lighting.GetColor_int_int -= LightColorDecorator;
            base.Unload();
        }

        private void DrawHook_TileEntities(On.Terraria.GameContent.Drawing.TileDrawing.orig_PostDrawTiles orig, TileDrawing self, bool solidLayer, bool forRenderTargets, bool intoRenderTargets)
        {
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);
            if (solidLayer == true && forRenderTargets)
            {
                DrawChunks();
            }
        }
        public Color LightColorDecorator(On.Terraria.Lighting.orig_GetColor_int_int orig, int i, int j)
        {
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

        public void EnsureSingleChunk(int i, int j)
        {
            if (chunks[i, j] == null)
                chunks[i, j] = new(i, j);
            chunks[i, j].EnsureRenderTargets();
            if (recentChunks.IndexOf(chunks[i, j]) == -1)
                _ = chunks[i, j].LoadChunk(ChunkFilename(chunks[i, j]));
        }

        public void EnsureChunks()
        {
            Vector2 topLeft = Main.Camera.ScaledPosition;
            Vector2 bottomRight = Main.Camera.ScaledSize + topLeft;
            for (int i = (int)(topLeft.X / PSChunk.CHUNK_WIDTH_INNER); i <= (int)(bottomRight.X / PSChunk.CHUNK_WIDTH_INNER); i++)
                for (int j = (int)(topLeft.Y / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)(bottomRight.Y / PSChunk.CHUNK_HEIGHT_INNER); j++)
                    EnsureSingleChunk(i, j);
        }

        public void MarkRecent(PSChunk chunk)
        {
            chunk.recentTimeTag = timeTag;
            if (recentChunks.IndexOf(chunk) == -1)
                recentChunks.Add(chunk);
        }

        public void UpdateChunks()
        {
            timeTag++;
            frameSeed = Main.rand.NextFloat();
            EnsureChunks();
            Vector2 topLeft = Main.Camera.ScaledPosition;
            Vector2 bottomRight = Main.Camera.ScaledSize + topLeft;
            for (int i = (int)(topLeft.X / PSChunk.CHUNK_WIDTH_INNER); i <= (int)(bottomRight.X / PSChunk.CHUNK_WIDTH_INNER); i++)
                for (int j = (int)(topLeft.Y / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)(bottomRight.Y / PSChunk.CHUNK_HEIGHT_INNER); j++)
                    MarkRecent(chunks[i, j]);
            recentChunks.Sort((PSChunk a, PSChunk b) =>
                a.recentTimeTag == b.recentTimeTag ? 0 : 
                a.recentTimeTag > b.recentTimeTag ? -1 : 1);
            while (recentChunks.Count > PREFERED_CHUNKS)
            {
                _ = recentChunks[^1].SaveChunk(ChunkFilename(recentChunks[^1]));
                chunks[recentChunks[^1].idx, recentChunks[^1].idy] = null;
                recentChunks.RemoveAt(recentChunks.Count - 1);
            }
            int count = recentChunks.Count;
            for (int i = 0; i < count; i++)
                recentChunks[i].Update();
        }

        public void DrawChunks()
        {
            EnsureChunks();
            Vector2 topLeft = Main.Camera.ScaledPosition;
            Vector2 bottomRight = Main.Camera.ScaledSize + topLeft;
            for (int i = (int)(topLeft.X / PSChunk.CHUNK_WIDTH_INNER); i <= (int)(bottomRight.X / PSChunk.CHUNK_WIDTH_INNER); i++)
                for (int j = (int)(topLeft.Y / PSChunk.CHUNK_HEIGHT_INNER); j <= (int)(bottomRight.Y / PSChunk.CHUNK_HEIGHT_INNER); j++)
                    chunks[i, j].Draw();
        }

        public override void PostUpdateDusts()
        {
            UpdateChunks();
            base.PostUpdateDusts();
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