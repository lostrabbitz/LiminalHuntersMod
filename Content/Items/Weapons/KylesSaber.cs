using System;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    /// <summary>
    /// Textures expected next to this file:
    ///   KylesSaber.png       (69x69, handle bottom-left, blade top-right)
    ///   KylesSaber_Glow.png  (69x69, blade glow only, same layout)
    /// </summary>
    public class KylesSaber : ModItem
    {
        public const string GlowTexturePath = "LiminalHuntersMod/Content/Items/Weapons/KylesSaber_Glow";

        public const int SwingTicks = 30;        // 60 ticks/s -> 2 swings per second
        public const int ParryStanceTicks = 36;  // 0.6 s stance (matches the effect length)

        // ---------- Parry pose tuning ----------
        /// <summary>Arm angle above horizontal, towards the facing direction.</summary>
        public const float ParryArmAngleDeg = 45f;
        /// <summary>Blade direction relative to facing. 90 = straight down, 45 = down + forward.</summary>
        public const float ParryBladeAngleDeg = 90f;
        /// <summary>Shoulder to hand distance in pixels.</summary>
        public const float ArmLength = 24f;
        /// <summary>Shoulder position relative to MountedCenter (X is mirrored by facing).</summary>
        public static readonly Vector2 ShoulderOffset = new Vector2(-2f, -8f);
        /// <summary>
        /// The pivot: where the player's hand grips the sprite, measured from the sprite's
        /// BOTTOM-LEFT corner (+X right, +Y up). The handle lives in the bottom-left of the 69x69 sprite.
        /// </summary>
        public static readonly Vector2 GripFromCorner = new Vector2(10f, 10f);

        public override void SetDefaults()
        {
            Item.width = 69;
            Item.height = 69;
            Item.scale = 1.2f;

            Item.DamageType = ModContent.GetInstance<CineticDamageClass>();
            Item.damage = 120;
            Item.knockBack = 5f;
            Item.crit = 0;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = SwingTicks;
            Item.useAnimation = SwingTicks;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item15;

            Item.rare = ItemRarityID.Cyan;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            KylesSaberPlayer kyle = player.GetModPlayer<KylesSaberPlayer>();

            //Aqui va Lost :D

            if (player.altFunctionUse == 2) // right click = parry
            {
                if (kyle.ParryCooldown > 0)
                    return false;

                Item.useTime = ParryStanceTicks;
                Item.useAnimation = ParryStanceTicks;
                Item.noMelee = true;
                Item.autoReuse = false;
                Item.UseSound = null; // parry sound is played by KylesSaberPlayer
            }
            else // left click = normal swing
            {
                Item.useTime = SwingTicks;
                Item.useAnimation = SwingTicks;
                Item.noMelee = false;
                Item.autoReuse = true;
                Item.UseSound = SoundID.Item15;
            }

            return true;
        }

        

        public override bool? UseItem(Player player)
        {
            // Resolve the parry once, only on the owning client.
            if (player.altFunctionUse == 2 && player.whoAmI == Main.myPlayer)
                player.GetModPlayer<KylesSaberPlayer>().DoParry(Item);

            return true;
        }

        // Runs after vanilla applies the swing, so we only override it while parrying.
        // When the stance ends this stops running and the normal swing pivot is back automatically.
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (!player.GetModPlayer<KylesSaberPlayer>().IsParrying)
                return;

            GetParryPose(player, out _, out float itemRotation, out Vector2 itemLocation);
            player.itemRotation = itemRotation;
            player.itemLocation = itemLocation;
        }

        public override void UseItemFrame(Player player)
        {
            if (!player.GetModPlayer<KylesSaberPlayer>().IsParrying)
                return;

            GetParryPose(player, out float armRotation, out _, out _);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        /// <summary>
        /// Computes the parry pose. Vanilla draws a held sword rotated around the sprite's bottom-left
        /// corner (bottom-right when facing left). Since we want the HANDLE in the hand, we place that
        /// corner at (hand - rotated grip offset), which moves the effective pivot onto the handle.
        /// </summary>
        public static void GetParryPose(Player player, out float armRotation, out float itemRotation, out Vector2 itemLocation)
        {
            int dir = player.direction;

            // Arm: 45 degrees up and forward.
            float armAngle = MathHelper.ToRadians(ParryArmAngleDeg);
            Vector2 armDir = new Vector2(dir * (float)Math.Cos(armAngle), -(float)Math.Sin(armAngle));
            armRotation = armDir.ToRotation() - MathHelper.PiOver2; // composite arm: 0 = pointing down

            Vector2 shoulder = player.MountedCenter + new Vector2(ShoulderOffset.X * dir, ShoulderOffset.Y + player.gfxOffY);
            Vector2 hand = shoulder + armDir * ArmLength;

            // Sprite blade points up-right at rotation 0 (-45 deg). Mirrored when facing left (-135 deg).
            float blade = MathHelper.ToRadians(ParryBladeAngleDeg);
            float bladeWorldAngle = dir == 1 ? blade : MathHelper.Pi - blade;
            itemRotation = bladeWorldAngle + (dir == 1 ? MathHelper.PiOver4 : MathHelper.PiOver4 * 3f);

            // Grip point relative to the draw origin, then rotated.
            Vector2 gripLocal = new Vector2(dir * GripFromCorner.X, -GripFromCorner.Y);
            itemLocation = hand - gripLocal.RotatedBy(itemRotation);
        }
    }
}