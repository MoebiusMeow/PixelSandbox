using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Weapons
{
    public class SandBomb : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 99;
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.WetBomb);
            Item.rare = ItemRarityID.Yellow;
            // Item.shoot = ModContent.ProjectileType<TriggerProjectileBase>();
        }
    }
}