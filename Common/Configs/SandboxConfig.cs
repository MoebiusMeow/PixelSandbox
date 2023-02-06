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
    [Label("$Mods.PixelSandbox.Config.Configs")]
    public class SandboxConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("$Mods.PixelSandbox.Config.Performance")]
        [Label("$Mods.PixelSandbox.Config.ChunkCount.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.ChunkCount.Tooltip")]
        [DefaultValue(20)]
        [Range(1, 100)]
        [Increment(1)]
        public int ChunkCount;

        [Label("$Mods.PixelSandbox.Config.UnloadDelay.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.UnloadDelay.Tooltip")]
        [DefaultValue(3)]
        [Range(3, 60)]
        [Increment(1)]
        public int UnloadDelay;

        [Label("$Mods.PixelSandbox.Config.ScreenPadding.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.ScreenPadding.Tooltip")]
        [Range(0, 600)]
        [Increment(1)]
        [DefaultValue(60)]
        public int ScreenPadding;

        [Label("$Mods.PixelSandbox.Config.LightingInterval.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.LightingInterval.Tooltip")]
        [Range(10f, 100f)]
        [Increment(1f)]
        [DefaultValue(100f)]
        public float LightingInterval;

        [Header("$Mods.PixelSandbox.Config.Debug")]

        [Label("$Mods.PixelSandbox.Config.EnableChunkDisplay.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.EnableChunkDisplay.Tooltip")]
        public bool EnableChunkDisplay;

        [Label("$Mods.PixelSandbox.Config.EnableDebugMessage.Label")]
        [Tooltip("$Mods.PixelSandbox.Config.EnableDebugMessage.Tooltip")]
        public bool EnableDebug;

        internal void OnDeserializedMethod(StreamingContext context) {
            UnloadDelay = Utils.Clamp(UnloadDelay, 3, 60);
            ScreenPadding = Utils.Clamp(ScreenPadding, 0, 600);
            LightingInterval = Utils.Clamp(LightingInterval, 1, 8);
        }
    }
}