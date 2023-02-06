using System;
using Terraria;

namespace PixelSandbox.Components
{
    public class BehaviorHook : ISandboxComponent
    {
        public Action<ISandboxObject> Update { get; set; } = null;
        public Action<ISandboxObject, Player> PlayerUseAnimation { get; set; } = null;
    }
}