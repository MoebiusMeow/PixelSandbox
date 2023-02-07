using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelSandbox.Contents.Items.Cleaners;
using PixelSandbox.Contents.Items.Materials;
using PixelSandbox.Contents.Items.Placeable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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
    public class SandBag : ModItem
    {
        public static float SingleUse => 0.23f;
        public float remain;

		public override void SaveData(TagCompound tag) {
			tag["remain"] = remain;
		}

		public override void LoadData(TagCompound tag) {
			tag.TryGet<float>("remain", out remain);
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(remain);
		}

		public override void NetReceive(BinaryReader reader) {
			remain = reader.ReadSingle();
		}

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }


        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Tile tile = Main.tile[Main.MouseWorld.ToTileCoordinates()];
                if (tile != null && tile.TileType == TileID.Extractinator)
                    return base.UseItem(player);
                if (player.ItemAnimationJustStarted)
                {
                    var result = Item.stack * remain;
                    Item.stack -= ConsumeItem(player).ToInt();
                    Vector2 direction = Main.MouseWorld - player.Center;
                    if (direction.Length() > 0)
                        direction.Normalize();
                    Projectile proj = Projectile.NewProjectileDirect(new EntitySource_ItemUse(Item, Item), player.Center, Item.shootSpeed * direction,
                        ModContent.ProjectileType<SandBagProjectile>(), 0, 0, player.whoAmI);
                    proj.timeLeft = (int)(proj.timeLeft * MathF.Min(result, SingleUse) / SingleUse);
                    // var rem = result - MathF.Floor(result);
                    // lastVisualScale = rem == 0 ? 2 : 1 + rem + SingleUse;
                }
            }
            return base.UseItem(player);
        }

        public override void OnCreate(ItemCreationContext context)
        {
            base.OnCreate(context);
        }

        public override bool CanUseItem(Player player)
        {
            // var result = Item.stack * remain;
            // if (result + 1e-3f < SingleUse) return false;
            return base.CanUseItem(player);
        }

        public override bool CanStack(Item item2)
        {
            var other = item2.ModItem as SandBag;
            if (item2.stack == 0)
                return true;
            var result = (Item.stack * remain + item2.stack * other.remain);
            int origStack = Item.stack + item2.stack;
            int newStack = (int)MathF.Ceiling(result);
            remain = newStack <= 0 ? 0 : result / newStack;
            Item.stack += newStack - origStack;
            return true;
        }

        public override bool CanStackInWorld(Item item2)
        {
            return CanStack(item2);
        }

        public override bool ConsumeItem(Player player) {
            var result = Item.stack * remain;
            if (MathF.Ceiling(result - SingleUse) < MathF.Ceiling(result))
            {
                remain = Item.stack <= 1 ? 0 : (result - SingleUse) / (Item.stack - 1);
                return true;
            }
            remain = (result - SingleUse) / Item.stack;
            return false;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Vector2 spriteSize = frame.Size() * scale;

			spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Vector2(position.X, position.Y + spriteSize.Y * 0.9f),
				new Rectangle(0, 0, 1, 1), Color.Red, 0f, Vector2.Zero,
				new Vector2(spriteSize.X, 2f),
				SpriteEffects.None, 0);

            float rem = Item.stack * remain - MathF.Floor(Item.stack * remain);
            // lastVisualScale = MathHelper.Lerp(lastVisualScale, (rem == 0 ? 1 : rem) + 1, 0.1f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Vector2(position.X, position.Y + spriteSize.Y * 0.9f),
				new Rectangle(0, 0, 1, 1), Color.LightGreen, 0f, Vector2.Zero,
				new Vector2(spriteSize.X * (rem == 0 ? 1 : rem), 2f),
				SpriteEffects.None, 0);
		}


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
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = 1;
            Item.rare = ItemRarityID.Yellow;
            Item.shootSpeed = 10;
            // Item.shoot = ModContent.ProjectileType<SandBagProjectile>();
            Item.autoReuse = true;
            Item.consumable = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            remain = 1;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(tooltips);
        }

        public override void ExtractinatorUse(ref int resultType, ref int resultStack)
        {
            // 这个函数执行时的Item不是实际Item
            // 不能用于判断实例情况
            float value = Main.rand.NextFloat() * 100;
            if ((value -= 0.5f) <= 0)
            {
                resultType = Main.rand.Next(ItemID.GoldBird, ItemID.GoldWorm + 1);
                resultStack = 1;
                return;
            }

            if ((value -= 0.2f) <= 0)
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

            if ((value -= 1.5f) <= 0)
            {
                switch (Main.rand.Next(1))
                {
                    case 0: resultType = ItemID.PaperAirplaneA; break;
                    case 1: resultType = ItemID.PaperAirplaneB; break;
                }
                resultStack = Main.rand.Next(2, 6);
                return;
            }

            if ((value -= 1.0f) <= 0)
            {
                // 奇怪的雕像
                resultType = ModContent.ItemType<StrangeStatue>();
                resultStack = 1;
                return;
            }

            if ((value -= 5.0f) <= 0)
            {
                // 破旧魔法书
                resultType = ModContent.ItemType<TatteredSpellTome>();
                resultStack = 1;
                return;
            }

            if ((value -= 6.0f) <= 0)
            {
                // Sand -> Sandwiches
                // Reasonable!
                resultType = ItemID.ShrimpPoBoy;
                resultStack = 1;
                return;
            }

            if ((value -= 7.0f) <= 0)
            {
                resultType = ItemID.Oyster;
                resultStack = 1;
                return;
            }

            resultType = ItemID.SandBlock;
            resultStack = Main.rand.Next(2, 5);
            return;
        }
    }

    public class SandBagProjectile : PSModProjectile
    {
        // public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.SandBallFalling}";

        public override string Texture => (typeof(SandBag).Namespace + "." + "SandBag").Replace('.', '/');

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.CloneDefaults(ProjectileID.SandBallFalling);
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 100;
            Projectile.width = Projectile.height = 32;
            DrawOriginOffsetX = 0;
            DrawOriginOffsetY = 0;
        }

        public override bool? CanCutTiles()
        {
            return false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Behavior.Update = GenerateSand;
            base.OnSpawn(source);
        }

        public override void AI()
        {
            Projectile.scale = Projectile.timeLeft * 0.01f;
            Projectile.velocity += Vector2.UnitY * Projectile.timeLeft * 0.01f * 0.4f;
            if (Projectile.velocity.Length() > 0)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathF.PI;
            if (Projectile.velocity.Y > 16)
                Projectile.velocity.Y = 16;
            // Projectile.alpha = 255;
            // Projectile.alpha = (int)MathHelper.Lerp(255, 0, Projectile.timeLeft * 0.01f);
            Projectile.velocity *= MathHelper.Lerp(1, Projectile.timeLeft * 0.01f, 0.1f);
            base.AI();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        protected Vector4[] data;

        public void GenerateSand(ISandboxObject sandboxObject)
        {
            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;

                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                var effectSize = PSSandboxSystem.Instance.effectSize;

                if (data == null || data.Length != effectSize.X * effectSize.Y)
                    data = new Vector4[effectSize.X * effectSize.Y];

                effectRTSwap.GetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
                for (int j = 0; j < effectSize.Y; j++)
                    for (int i = 0; i < effectSize.X; i++)
                    {
                        Vector2 uv = new Vector2(i / (float)effectSize.X, j / (float)effectSize.Y);
                        Vector4 value = data[i + j * effectSize.X];
                        if (value.W == 0)
                        {
                            data[i + j * effectSize.X] = new Vector4(
                                Color.Lerp(new Color(212, 192, 100), Color.Orange, 0)
                                .ToVector3() * MathHelper.Lerp(0.9f, 1.0f, Main.rand.NextFloat()), 1);
                        }
                    }
                effectRT.SetData(0, new Rectangle(0, 0, effectSize.X, effectSize.Y), data, 0, data.Length);
            };
            var sandProjectile = sandboxObject as SandBagProjectile;
            float radius = 4;
            Vector2 targetPos = sandProjectile.Projectile.Center;
            // targetPos -= Vector2.One * radius;

            if (sandProjectile.Projectile.timeLeft > 30)
                PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * radius * 2, callback);

        }
    }
}