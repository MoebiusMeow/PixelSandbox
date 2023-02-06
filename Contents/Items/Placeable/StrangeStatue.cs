using Microsoft.Xna.Framework;
using PixelSandbox.Contents.Tiles;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Placeable
{
    public class StrangeStatue : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<StrangeStatueTile>());
            Item.width = 32;
            Item.height = 32;
            Item.value = 10000;
            Item.rare = ItemRarityID.Green;
        }

    }
}