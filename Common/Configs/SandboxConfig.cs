using Microsoft.Xna.Framework;
using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PixelSandbox.Configs
{
    public class SandboxConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.PixelSandbox.Configs.Performance")]
        [DefaultValue(20)]
        [Range(1, 100)]
        [Increment(1)]
        public int ChunkCount;

        [DefaultValue(3)]
        [Range(3, 60)]
        [Increment(1)]
        public int UnloadDelay;

        [Range(0, 600)]
        [Increment(1)]
        [DefaultValue(60)]
        public int ScreenPadding;

        [Range(10f, 100f)]
        [Increment(1f)]
        [DefaultValue(100f)]
        public float LightingInterval;

        [Header("$Mods.PixelSandbox.Configs.Debug")]

        public bool EnableChunkDisplay;

        public bool EnableDebug;

        internal void OnDeserializedMethod(StreamingContext context) {
            UnloadDelay = Utils.Clamp(UnloadDelay, 3, 60);
            ScreenPadding = Utils.Clamp(ScreenPadding, 0, 600);
            LightingInterval = Utils.Clamp(LightingInterval, 1, 8);
        }
    }
}