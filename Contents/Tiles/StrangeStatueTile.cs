using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace PixelSandbox.Contents.Tiles
{
    public class StrangeStatueTile : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;

            DustType = DustID.ManaRegeneration;
            AdjTiles = new int[] { TileID.AlchemyTable, TileID.WorkBenches };

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 18 };
            TileObjectData.newTile.CoordinatePaddingFix = new Point16(0, 2);
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.StyleHorizontal = true;

            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            AddMapEntry(Color.Gray, CreateMapEntryName());
        }

        public override bool RightClick(int x, int y) {
            CombatText.NewText(new Rectangle(x * 16, y * 16, 16, 16), Color.Yellow, "What do you expect?");
            return true;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) {
            num = fail ? 1 : 3;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY) {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<Items.Placeable.StrangeStatue>());
        }
    }
}