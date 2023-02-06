using Microsoft.Xna.Framework;
using PixelSandbox.Components;
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
    public abstract class PSModItem : ModItem, ISandboxObject
    {
        public List<ISandboxComponent> components = new List<ISandboxComponent>();

        public BehaviorHook Behavior
        {
            get
            {
                var component = GetComponent(typeof(BehaviorHook));
                if (component == null)
                    AddComponent(component = new BehaviorHook());
                return (BehaviorHook)component;
            }
        }

        public ISandboxComponent GetComponent(Type T)
        {
            return components.Find((ISandboxComponent c) => c.GetType() == T);
        }

        public bool AddComponent(ISandboxComponent component)
        {
            if (GetComponent(component.GetType()) != null)
                return false;
            components.Add(component);
            return true;
        }

        public bool RemoveComponent(Type T)
        {
            ISandboxComponent component = GetComponent(T);
            if (component == null)
                return false;
            components.Remove(component);
            return true;
        }
    }
}