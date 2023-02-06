using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox.Contents.Items.Weapons
{
    public class RaidingErosionTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.maxStack = 1;
            Item.useTime = 30;
            Item.useAnimation = 20;
            Item.holdStyle = ItemHoldStyleID.None;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.useTurn = true;
            // Item.shoot = ModContent.ProjectileType<TriggerProjectileBase>();
        }
    }
}