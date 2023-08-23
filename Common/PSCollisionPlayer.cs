using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace PixelSandbox
{
    /// <summary>
    /// 从很久以前的动态平台系统移植了一部分过来
    /// 使用了大量临时变量来适配玩家移动，逻辑很复杂
    /// 可以理解为用PlayerHook当IL编辑来使
    /// </summary>
    public class PSCollisionPlayer : ModPlayer
    {
        Vector2 localOldPosition, localOldVelocity;
        Vector2 tmpVelocity;
        Vector2 localDelayedVelocity;
        Vector2 localTransmissionVelocity;
        int localCollided;
        bool localJumpAgain;
        bool localGrap;
        bool localClimb;
        float localClimbSpeed;
        bool localStandOnSand;

        public override void OnEnterWorld()
        {
            localCollided = -1;
        }

        public override void PostUpdateRunSpeeds()
        {
            localOldPosition = Player.oldPosition;
            tmpVelocity = Player.velocity;
            if (localJumpAgain)
            {
                Player.velocity.Y = 0;
            }
            localStandOnSand = false;
            PSCollisionHookSystem.Instance.noStepDown = false;
            if ((Player.velocity.Y >= 0 || (Player.controlUp && !Player.controlJump)) && !Player.mount.CanFly())
            {
                var sand = PSSandboxSystem.Instance.SoftSampleSand(Player.Bottom);
                if (sand.W > 0)
                {
                    localStandOnSand = true;
                    if (!Player.controlDown)
                        PSCollisionHookSystem.Instance.noStepDown = true;
                }
            }
        }

        public override void PostUpdateMiscEffects()
        {
            localGrap = false;
        }

        public override void PreUpdateMovement()
        {
            if (localJumpAgain)
            {
                Player.velocity.Y += tmpVelocity.Y;
            }
            localJumpAgain = false;
            UpdateSandboxCollisions();
            localOldVelocity = Player.velocity;
        }

        public override void FrameEffects()
        {
            tmpVelocity = Player.velocity;
            if (localCollided != -1)
            {
                if (Player.whoAmI == Main.myPlayer)
                    Main.SetCameraLerp(0.4f, 1);
                if (localJumpAgain)
                {
                    Player.velocity.Y = 0;
                    if (localClimb && MathF.Abs(Player.velocity.X) < localClimbSpeed)
                        Player.velocity.X = -localClimbSpeed * Player.direction;
                    if (!localClimb && !Player.controlRight && !Player.controlLeft)
                    {
                        Player.velocity.X = 0;
                        Player.legFrameCounter = 0;
                    }
                    for (int i = 3; i < Player.hideVisibleAccessory.Length; i++)
                        if (Player.armor[i].wingSlot == Player.wingsLogic && Player.hideVisibleAccessory[i])
                            Player.wings = 0;
                    if (Player.wings > 0)
                        Player.wingFrame = 0;
                }
            }
        }

        public override void PostUpdate()
        {
            if (localGrap) Player.legFrame.Y = 5 * Player.legFrame.Height;
            Player.velocity = tmpVelocity;
            // support vanilla collision
            if (tmpVelocity == localOldVelocity)
            {
                Player.velocity = localDelayedVelocity;
            }
        }

        private void UpdateSandboxCollisions()
        {
            bool fallThrough = Player.controlDown || Player.GoingDownWithGrapple;
            bool slimeFall = Player.mount.Type == MountID.Slime && Player.mount.Active;
            localCollided = -1;
            localDelayedVelocity = Player.velocity;
            Player.velocity += localTransmissionVelocity;
            localClimb = false;
            localClimbSpeed = 0;

            Vector2 velocityPostTrans = Player.velocity;
            Vector2 velocityAfterTile = Collision.TileCollision(Player.position, velocityPostTrans, Player.width, Player.height, true);

            Vector2 velocityAfterSand = velocityAfterTile;
            Vector2 target = Player.position + velocityAfterTile;
            if ((velocityAfterTile.Y > 0 || (Player.controlUp && !Player.controlJump)) && localStandOnSand)
            {
                if (!fallThrough)
                {
                    localCollided = 1;
                    Vector2 intend = velocityAfterSand;
                    if (Player.controlUp)
                        intend.Y = -MathF.Max(2, MathF.Abs(velocityAfterSand.X));
                    else
                        intend.Y = 0;

                    // 优先往斜上走，其次水平，最后往下
                    var testSand = PSSandboxSystem.Instance.SoftSampleSand(Player.Bottom + intend);
                    if (testSand.W <= 0 && intend.Y < 0)
                    {
                        intend.Y = 0;
                        testSand = PSSandboxSystem.Instance.SoftSampleSand(Player.Bottom + intend);
                    }
                    if (Player.controlUp && testSand.W > 0)
                    {
                        Player.gfxOffY = 0;
                        velocityAfterSand.Y = intend.Y;
                        localClimb = true;
                        localClimbSpeed = -intend.Y;
                    }
                    else if (testSand.W > 0)
                        velocityAfterSand.Y = intend.Y;
                    else
                        velocityAfterSand.Y = MathF.Max(2, MathF.Abs(velocityAfterSand.X));
                    target = Player.position + velocityAfterSand;
                }
                else
                    InflictFallDamage();
            }
            if (MathF.Abs(velocityAfterTile.X) < MathF.Abs(velocityAfterSand.X) - float.Epsilon)
            {
                velocityAfterSand = velocityAfterTile;
                localDelayedVelocity = velocityAfterSand;
                target = Player.position + velocityAfterTile;
            }
            else
                localDelayedVelocity = velocityAfterSand;

            if (localCollided != -1 && !Player.justJumped)
            {
                Vector2 N = -Vector2.UnitY;
                if (N.Y * Player.gravDir < 0)
                {
                    if (Player.mount.Type == MountID.Slime)
                        Player.velocity.Y *= 0.4f;
                    InflictFallDamage();
                    if (N.Y * Player.gravDir < -0.3f)
                        localJumpAgain = true;
                }
                Player.jump = 0;
            }

            // After collision
            if (localCollided != -1)
            {
                Vector2 stepV = target - Player.position;
                Vector2 step = Collision.TileCollision(Player.position, stepV, Player.width, Player.height, true);
                Player.velocity = localDelayedVelocity;
                Player.position += step - localDelayedVelocity;

                localDelayedVelocity -= localTransmissionVelocity;
                localTransmissionVelocity = Vector2.Zero;
            }
            else
            {
                localDelayedVelocity -= localTransmissionVelocity;
                localTransmissionVelocity = Vector2.Zero;
            }
        }

        public void InflictFallDamage()
        {
            int safe = 25 + Player.extraFall;
            int current = (int)(Player.position.Y / 16f) - Player.fallStart;
            if (Player.mount.CanFly() || Player.mount.Type == 1)
            {
                current = 0;
            }
            Player.mount.FatigueRecovery();
            if (Player.stoned)
                safe = 0;
            if (((Player.gravDir == 1f && current > safe) || (Player.gravDir == -1f && current < -safe)) && !Player.noFallDmg && Player.wingsLogic == 0)
            {
                for (int i = 3; i < 8 + Player.extraAccessorySlots; i++)
                    if (Player.armor[i].wingSlot > 0)
                        return;
                Player.immune = false;
                int damage = (int)((float)current * Player.gravDir - (float)safe) * 10;
                if (Player.mount.Active)
                {
                    damage = (int)((float)damage * Player.mount.FallDamage);
                }
                Player.Hurt(PlayerDeathReason.ByOther(0), damage, 0);
            }
            Player.fallStart = (int)(Player.position.Y / 16f);
        }
    }
}