using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PixelSandbox.Contents.Items.Cleaners
{
    public class VacuumCleanerWood : VacuumCleanerBase
    {
        public override string Texture => (GetType().Namespace + "." + Name).Replace('.', '/');
        public override SoundStyle EndSound => SoundID.Item32;

        public override float CleanerRadius => 50;
        public override float Centrifuge => 0.3f * Main.player[Item.playerIndexTheItemIsReservedFor].itemAnimation / Item.useAnimation;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.useTime = 30;
            Item.useAnimation = 10;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useStyle = ItemUseStyleID.MowTheLawn;
            Item.noMelee = true;
            Item.value = 1;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item32 with { Pitch = -0.1f };
            Item.autoReuse = true;
            Item.useTurn = true;
            sandCount = 0;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup("Wood", 10);
            recipe.Register();
        }
    }
}