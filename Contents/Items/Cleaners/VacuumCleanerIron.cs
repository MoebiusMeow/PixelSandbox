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
    public class VacuumCleanerIron : VacuumCleanerBase
    {
        public override string Texture => (GetType().Namespace + "." + "VacuumCleanerBase").Replace('.', '/');

        public override float CleanerRadius => 82;
        public override float Centrifuge => 0.2f * Main.player[Item.playerIndexTheItemIsReservedFor].itemAnimation / Item.useAnimation;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup("IronBar", 3);
            recipe.AddRecipeGroup("Sand", 10);
            recipe.AddIngredient(ModContent.ItemType<SandBag>());
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}