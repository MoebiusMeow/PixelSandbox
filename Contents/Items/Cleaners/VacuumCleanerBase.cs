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
    public abstract class VacuumCleanerBase : PSModItem
    {
        public override string Texture => (GetType().Namespace + "." + "VacuumCleanerBase").Replace('.', '/');
        public virtual SoundStyle EndSound => SoundID.Item23;

        public virtual float CleanerRadius => 30;
        public virtual float Centrifuge => 0.20f;

        public static float SandPerBag => 3000;
        public float sandCount;

        public int sandBagFilled => (int)MathF.Floor(sandCount / SandPerBag);
        public float sandInNextBag => sandCount - sandBagFilled * SandPerBag;

        public VacuumCleanerBase()
        {
            Behavior.PlayerUseAnimation = Vacuum;
        }

        protected Vector4[] data, newData;
        protected int[] dupCount, dupLast;

        public virtual void Vacuum(ISandboxObject sandboxObject, Player player)
        {
            float sandIncrease = 0;
            // ����ֲ���������ȡ����ֵ
            float radius = 0.50f;

            var callback = (GraphicsDevice device, Vector2 topLeft, Vector2 bottomRight) =>
            {
                Vector2 center = Vector2.One * 0.5f;

                // �����ΪCPU��������������
                var effectRT = PSSandboxSystem.Instance.effectRT;
                var effectRTSwap = PSSandboxSystem.Instance.effectRTSwap;
                if (data == null || data.Length != effectRT.Width * effectRT.Height)
                    data = new Vector4[effectRT.Width * effectRT.Height];
                if (newData == null || newData.Length != effectRT.Width * effectRT.Height)
                    newData = new Vector4[effectRT.Width * effectRT.Height];
                if (dupCount == null || dupCount.Length != effectRT.Width * effectRT.Height)
                    dupCount = new int[effectRT.Width * effectRT.Height];
                if (dupLast == null || dupLast.Length != effectRT.Width * effectRT.Height)
                    dupLast = new int[effectRT.Width * effectRT.Height];
                for (int i = 0; i < newData.Length; i++)
                {
                    dupCount[i] = 0;
                    dupLast[i] = -1;
                    newData[i] = Vector4.Zero;
                }

                effectRTSwap.GetData(data);
                for (int j = 0; j < effectRT.Height; j++)
                    for (int i = 0; i < effectRT.Width; i++)
                    {
                        Vector2 uv = new Vector2(i / (float)effectRT.Width, j / (float)effectRT.Height);
                        Vector4 value = data[i + j * effectRT.Width];
                        if (value.W > 0)
                        {
                            float dist = (uv - center).Length();
                            Vector2 targetUV = uv;
                            if (dist <= radius)
                            {
                                targetUV = center + (uv - center).
                                    RotatedBy((1 - dist / radius) * MathF.Cos(dist / radius * 2.5f) * player.direction) *
                                    MathHelper.Lerp(1.0f, MathF.Pow(dist / radius, 2), 0.1f);
                            }
                            if ((targetUV - center).Length() <= radius * Centrifuge)
                                targetUV = -Vector2.One;
                            Point t = new Point((int)MathF.Round(targetUV.X * effectRT.Width), (int)MathF.Round(targetUV.Y * effectRT.Height));
                            int targetIndex = t.X + t.Y * effectRT.Width;
                            if (!effectRT.Frame().Contains(t))
                                sandIncrease += 1;
                            else if (data[targetIndex].W == 0)
                            {
                                // ���Ŀ��λ���ڵ�ǰʱ��Ҳû�б�ռ�ò������ƶ�������Ŀ��λ�õ���������ƶ�ʧ�ܾ��޴���ȥ
                                // �����Ǹ��������
                                // �ȼ��ڣ�ȥ��ͬһ��Ŀ��λ�õ������еȸ��ʳ�ȡ����һ���ɹ�����������ԭλ
                                dupCount[targetIndex] += 1;
                                if (Main.rand.NextBool(dupCount[targetIndex]))
                                {
                                    newData[targetIndex] = data[i + j * effectRT.Width];
                                    if (dupLast[targetIndex] != -1)
                                        newData[dupLast[targetIndex]] = data[dupLast[targetIndex]];
                                    dupLast[targetIndex] = i + j * effectRT.Width;
                                }
                            }
                            else
                            {
                                // �ƶ�ʧ�ܣ�����ԭλ
                                newData[i + j * effectRT.Width] = data[i + j * effectRT.Width];
                            }
                        }
                    }
                effectRT.SetData(newData);

                // ��Ҳ����ʹ��GPU��һ������������Ť��
                // ��������������ģ�����𣨻������ӱ��/���٣�
                if (false)
                {
                    var behaviorShader = PSSandboxSystem.behaviorShader;
                    device.SetRenderTarget(effectRT);
                    device.Clear(Color.Transparent);
                    BlendState blendState = new BlendState();
                    blendState.AlphaBlendFunction = BlendFunction.Max;
                    blendState.ColorBlendFunction = BlendFunction.Add;
                    blendState.AlphaSourceBlend = Blend.One;
                    blendState.AlphaDestinationBlend = Blend.One;
                    blendState.ColorSourceBlend = Blend.One;
                    blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;

                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                    // Main.spriteBatch.Draw(Mod.Assets.Request<Texture2D>("Contents/Projectiles/LightningRitual", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, Vector2.Zero, null, Color.White, 0, Vector2.Zero, effectRT.Width / 408f, SpriteEffects.None, 0);
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
                    behaviorShader.Parameters["uTex1"].SetValue(effectRTSwap);
                    behaviorShader.Parameters["uCircleCenter"].SetValue(center);
                    behaviorShader.Parameters["uCircleRadius"].SetValue(radius);
                    behaviorShader.Parameters["uCircleCentrifuge"].SetValue(Centrifuge);
                    behaviorShader.Parameters["uCircleRotation"].SetValue(1.2f * player.direction);
                    behaviorShader.CurrentTechnique.Passes["WhiteHole"].Apply();
                    // behaviorShader.CurrentTechnique.Passes["WhiteHole"].Apply();
                    Main.spriteBatch.Draw(effectRTSwap, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                }
            };
            Vector2 targetPos = player.Bottom + new Vector2(player.Directions.X * 10, -18 - radius * Centrifuge * CleanerRadius);
            targetPos -= Vector2.One * CleanerRadius;

            PSSandboxSystem.Instance.UpdateChunksEffect(targetPos, targetPos + Vector2.One * CleanerRadius * 2, callback);

            var cleaner = sandboxObject as VacuumCleanerBase;
            int lastBags = cleaner.sandBagFilled;
            cleaner.sandCount += sandIncrease;
            if (!player.controlUseItem && player.itemAnimation == 1)
                SoundEngine.PlaySound(EndSound, player.Center);
            if (cleaner.sandBagFilled > lastBags)
            {
                var message = "{0} ".FormatWith(cleaner.sandBagFilled) +
                    PixelSandbox.ModTranslate(cleaner.sandBagFilled > 1 ? "SandBagsFilledHint" : "SandBagFilledHint", "Misc.");
                int id = CombatText.NewText(player.getRect(), Color.DarkOrange, message, true);
                SoundEngine.PlaySound(SoundID.Item1 with { MaxInstances = 1 }, player.Center);
            }
        }

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
            Item.rare = ItemRarityID.Yellow;
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
            tooltips.ForEach(line => 
            {
                if (line.Text.Contains(":sandbag:"))
                    line.Text = line.Text.Replace(":sandbag:", $"[i:{ModContent.ItemType<SandBag>()}]");
                if (line.Text.Contains(":sandbag_stack:") && sandBagFilled > 0)
                    line.Text = line.Text.Replace(":sandbag_stack:", $"[i/s{sandBagFilled}:{ModContent.ItemType<SandBag>()}]");
            });
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