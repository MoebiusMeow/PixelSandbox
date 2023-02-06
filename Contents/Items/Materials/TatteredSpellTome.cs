using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Materials
{
    public class TatteredSpellTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 99;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 9999;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.holdStyle = ItemHoldStyleID.None;
            Item.useStyle = ItemUseStyleID.None;
            Item.value = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.useTurn = true;
        }
    }
}