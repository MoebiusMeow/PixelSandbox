using Humanizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PixelSandbox.Contents.Items
{
    public class VacuumCleaner : ModItem
    {
        public static float SandPerBag => 10000;
        public float sandCount;

        public int sandBagFilled => (int)MathF.Floor(sandCount / SandPerBag);
        public float sandInNextBag => sandCount - sandBagFilled * SandPerBag;

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.useTime = 10;
            Item.height = 22;
            Item.width = 32;
            Item.useAnimation = 30;
            Item.holdStyle = ItemHoldStyleID.HoldFront;
            Item.useStyle = ItemUseStyleID.MowTheLawn;
            Item.noMelee = true;
            Item.value = 1;
            Item.rare = ItemRarityID.Expert;
            Item.UseSound = SoundID.Item13 with { Pitch = -0.1f };
            Item.autoReuse = true;
            Item.useTurn = true;
            sandCount = 0;
        }

        public override void HoldStyle(Player player, Rectangle heldItemFrame)
        {
            player.itemLocation.Y += 18;
            player.itemLocation.X -= 8 * player.direction;
        }

        public override void UseItemFrame(Player player)
        {
            player.itemLocation.Y += 18;
            player.itemLocation.X -= 8 * player.direction;
        }

        public override void SaveData(TagCompound tag)
        {
            tag.Set("SandCount", sandCount);
            base.SaveData(tag);
        }

        public override void LoadData(TagCompound tag)
        {
            tag.TryGet("SandCount", out sandCount);
            base.LoadData(tag);
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(sandCount);
            base.NetSend(writer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            sandCount = reader.ReadSingle();
            base.NetReceive(reader);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine
            (
                Mod, "SandBagFilled",
                PixelSandbox.ModTranslate("SandBagContains", "Misc.") +
                    ": [c/FFFF00:{0} ".FormatWith((int)sandBagFilled) +
                    PixelSandbox.ModTranslate(sandBagFilled > 1 ? "SandBagsFilled" : "SandBagFilled", "Misc.") + "]"
            ));
            tooltips.Add(new TooltipLine
            (
                Mod, "SandInNextBag",
                PixelSandbox.ModTranslate("SandBagCurrent", "Misc.") +
                    ": [c/FFFF00:{0:0.00}% ".FormatWith(100 * sandInNextBag / SandPerBag) +
                    PixelSandbox.ModTranslate("SandBagCurrentFilled", "Misc.") + "]"
            ));
            base.ModifyTooltips(tooltips);
        }

        public override void OnConsumeItem(Player player)
        {
            Item.stack += 1;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            if (sandBagFilled > 0)
            {
                Item.NewItem(new EntitySource_ItemOpen(Item, Type), player.getRect(), ModContent.ItemType<SandBag>(), sandBagFilled, default, default, true);
                sandCount = sandInNextBag;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup("IronBar", 3);
            recipe.AddRecipeGroup("Sand", 10);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}