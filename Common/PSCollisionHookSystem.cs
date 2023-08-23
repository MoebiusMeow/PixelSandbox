using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// <summary>
    /// 仅仅用来在沙子碰撞中阻止自动走下台阶的功能
    /// </summary>
    public class PSCollisionHookSystem : ModSystem
    {
        public static PSCollisionHookSystem Instance => _instance;
        private static PSCollisionHookSystem _instance;

        public bool noStepDown = false;

        public PSCollisionHookSystem()
        {
            _instance = this;
        }

        public override void Load()
        {
            Terraria.On_Collision.StepDown += Collision_StepDown;
            base.Load();
        }

        private void Collision_StepDown(Terraria.On_Collision.orig_StepDown orig, ref Vector2 position, ref Vector2 velocity, int width, int height, ref float stepSpeed, ref float gfxOffY, int gravDir, bool waterWalk)
        {
            if (!noStepDown)
                orig(ref position, ref velocity, width, height, ref stepSpeed, ref gfxOffY, gravDir, waterWalk);
        }


        public override void Unload()
        {
            base.Unload();
        }
    }

}