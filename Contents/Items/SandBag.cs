using Microsoft.Xna.Framework;
using PixelSandbox.Contents.Items.Materials;
using PixelSandbox.Contents.Items.Placeable;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items
{
    public class SandBag : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 999;
            ItemID.Sets.ExtractinatorMode[Item.type] = Item.type;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 9999;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.holdStyle = ItemHoldStyleID.None;
            Item.useStyle = ItemUseStyleID.Thrust;
            Item.value = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.useTurn = true;
        }

        public override void ExtractinatorUse(ref int resultType, ref int resultStack)
        {
            int value = Main.rand.Next(100);
            if (value == 0)
            {
                resultType = Main.rand.Next(ItemID.GoldBird, ItemID.GoldWorm + 1);
                resultStack = 1;
                return;
            }
            else if (value <= 1)
            {
                // 大喵时装（大雾）
                switch (Main.rand.Next(3))
                {
                    case 0: resultType = ItemID.CatEars; break;
                    case 1: resultType = ItemID.LamiaShirt; break;
                    case 3: resultType = ItemID.Toolbelt; break;
                }
                resultStack = 1;
                return;
            }
            else if (value <= 3)
            {
                switch (Main.rand.Next(2))
                {
                    case 0: resultType = ItemID.PaperAirplaneA; break;
                    case 1: resultType = ItemID.PaperAirplaneB; break;
                }
                resultStack = Main.rand.Next(2, 6);
                return;
            }
            else if (value <= 5)
            {
                // 奇怪的雕像
                resultType = ModContent.ItemType<StrangeStatue>();
                resultStack = 1;
                return;
            }
            else if (value <= 10)
            {
                // 破旧魔法书
                resultType = ModContent.ItemType<TatteredSpellTome>();
                resultStack = 1;
                return;
            }
            else if (value <= 20)
            {
                // Sand -> Sandwiches
                // Reasonable!
                resultType = ItemID.ShrimpPoBoy;
                resultStack = 1;
                return;
            }
            else if (value <= 40)
            {
                resultType = ItemID.Oyster;
                resultStack = 1;
                return;
            }
            else
            {
                resultType = ItemID.SandBlock;
                resultStack = Main.rand.Next(5, 20);
                return;
            }
        }
    }
}