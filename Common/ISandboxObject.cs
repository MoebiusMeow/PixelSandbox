using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox
{
    public interface ISandboxObject
    {
        public ISandboxComponent GetComponent(Type T);
        public bool AddComponent(ISandboxComponent component);
        public bool RemoveComponent(Type T);
    }
}