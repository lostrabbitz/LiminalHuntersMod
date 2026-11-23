using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;

namespace LiminalHuntersMod.Content.Projectiles.Weapons
{
    /// <summary>
    /// D20 Grenade Projectile:
    /// - Rotates and bounces with grenade physics
    /// - Explodes after 2 seconds (120 ticks)
    /// - Explodes IMMEDIATELY if an enemy approaches the blast radius (100 pixels / ~6.25 tiles)
    /// - On explosion: rolls 1-20, pops a large CombatText (1: Red, 2-19: White, 20: Golden)
    /// - Roll 1: Deals 2x damage to PLAYER (4x if crit)
    /// - Roll 2-10: Adds roll to total damage
    /// - Roll 11-19: Adds roll + floor(roll / 3) to total damage
    /// - Roll 20: Multiplies damage (2x, or 4x if crit)
    /// </summary>
    public class D20Projectile : ModProjectile
    {
        // 2 seconds at 60 updates per second = 120 ticks
        private const int MaxFuseTicks = 120;
        private const float BlastRadius = 70f; // Detection and explosion radius in pixels

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<ChanceDamageClass>();
            Projectile.penetrate = -1; // -1 allows it to explode and hit all enemies in blast radius
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
        }

        public override void AI()
        {
            // 1. Grenade Gravity & Physics
            Projectile.velocity.Y += 0.22f; // Gravity
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f; // Terminal velocity
            }

            // Air resistance
            Projectile.velocity.X *= 0.99f;

            // Rotation proportional to horizontal velocity
            Projectile.rotation += Projectile.velocity.X * 0.08f;

            // Small trail dust for liminal dice flair
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleCrystalShard, 0f, 0f, 120, default, 0.8f);
                dust.noGravity = true;
                dust.velocity *= 0.3f;
            }

            // 2. Fuse Timer (3 seconds)
            Projectile.ai[0]++;
            if (Projectile.ai[0] >= MaxFuseTicks)
            {
                Projectile.Kill(); // Explode on 3 second timeout!
                return;
            }

            // 3. Proximity Detonation:
            // "If an enemy approaches the blast radius, it should explode regardless of the 3 seconds."
            for (int i = 0; i < Main.maxNPCs; i++)
{
                NPC target = Main.npc[i];

                if (!target.active || target.friendly)
                    continue;

                bool isDummy = target.type == NPCID.TargetDummy;
                bool validEnemy = target.CanBeChasedBy(this) && !target.dontTakeDamage;

                if (!validEnemy && !isDummy)
                    continue;

                if (Vector2.Distance(Projectile.Center, target.Center) <= BlastRadius)
                {
                    Projectile.Kill();
                    return;
                }
            }
        }

        // Bouncing physics on hitting tiles
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Bounce horizontally
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.60f; // 60% elasticity
            }

            // Bounce vertically
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.50f; // 50% elasticity
            }

            // Add extra rotation kick on bounce
            Projectile.rotation += (float)(Main.rand.NextDouble() - 0.5) * 0.4f;

            // Return false so projectile DOES NOT die on collision with tiles; it bounces!
            return false;
        }

        // Explodes upon death (timer elapsed or proximity triggered)
        public override void OnKill(int timeLeft)
        {
            // Audio: Explosion sound + dice clink
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position); // Grenade explosion sound

            // Visual explosion dust particles
            CreateExplosionVisuals();

            // 1. Roll a random number between 1 and 20
            int roll = Main.rand.Next(1, 21);

            // 2. Choose CombatText color according to prompt requirements:
            // "1 is red, any other number is white and 20 is golden"
            Color textColor;
            if (roll == 1)
            {
                textColor = new Color(255, 45, 45); // Red (Critical Failure)
            }
            else if (roll == 20)
            {
                textColor = new Color(255, 215, 0); // Golden (Natural 20)
            }
            else
            {
                textColor = Color.White; // 2 to 19 is White
            }

            // 3. Pop the CombatText above the explosion:
            // "pop a number between 1 to 20 just like the damage numbers but I want it to be bigger"
            // dramatic: true creates an enlarged, prominent floating combat text with scale animation!
            Rectangle textRect = new Rectangle((int)Projectile.Center.X - 10, (int)Projectile.Center.Y - 25, 20, 20);
            CombatText.NewText(textRect, textColor, roll.ToString(), dramatic: true, dot: false);

            // 4. Calculate Critical Strike status
            Player player = Main.player[Projectile.owner];
            bool isCrit = Main.rand.Next(100) < (player.GetCritChance<ChanceDamageClass>() + player.GetCritChance(DamageClass.Generic));

            // Base damage from weapon + chance damage scaling
            int baseDamage = Projectile.damage; // 40 base

            // 5. Apply the exact prompt damage rules:
            if (roll == 1)
            {
                // RULE: "If its 1 it should do double the damage at the player (if its crit, its double that)."
                // int selfDamage = baseDamage * 2;
                int selfDamage = 2;
                if (isCrit)
                {
                    // selfDamage *= 2; // 4x base damage to player if critical strike!
                    selfDamage = 4;
                }

                // Inflict damage to the throwing player
                if (player.active && !player.dead && Projectile.owner == Main.myPlayer)
                {
                    // Create hurt info (tModLoader 1.4.4+ syntax)
                    Player.HurtModifiers hurtModifiers = new Player.HurtModifiers();
                    if (isCrit)
                    {
                        hurtModifiers.ScalingArmorPenetration += 0.5f;
                    }

                    // Hurt the player with a custom death reason
                    PlayerDeathReason reason = PlayerDeathReason.ByCustomReason(player.name + " rolled a Critical Failure (1) with a D20!");
                    player.Hurt(reason, selfDamage, Projectile.direction, false, false, -1, false);

                    // Extra red screen shake / flash feedback
                    CombatText.NewText(player.getRect(), Color.Crimson, "OUCH!", dramatic: true);
                }

                // Roll 1 does not harm enemies - it backfired!
                return;
            }
            
            if (roll == 20) {
                CombatText.NewText(player.getRect(), Color.Yellow, "PERFECT!!", dramatic: true);
            }

            int finalDamage = baseDamage;

            if (roll >= 2 && roll <= 10)
            {
                // RULE: "2 to 10 just adds that number to the total damage."
                finalDamage = baseDamage + roll;
            }
            else if (roll >= 11 && roll <= 19)
            {
                // RULE: "11 to 19 adds that number to the total damage plus 1/3th of the number rounded down."
                int oneThirdRoundedDown = (int)Math.Floor(roll / 3f);
                finalDamage = baseDamage + roll + oneThirdRoundedDown;
            }
            else if (roll == 20)
            {
                // RULE: "20 multiplies the damage (it its crit, double that)"
                finalDamage = baseDamage * 2; // 2x multiplier for Natural 20
            }

            // Apply Critical Strike doubling to final damage if rolled critical
            if (isCrit)
            {
                finalDamage *= 2; // If it's crit, double that! (For Nat 20, 2x * 2 = 4x total!)
            }

            // 6. Inflict Blast Damage to all enemies in the blast radius
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];

                if (!target.active || target.friendly)
                    continue;

                // Real enemies, or the target dummy (which fails CanBeChasedBy)
                bool isDummy = target.type == NPCID.TargetDummy;
                bool validEnemy = target.CanBeChasedBy(this) && !target.dontTakeDamage;

                if (!validEnemy && !isDummy)
                    continue;

                float dist = Vector2.Distance(Projectile.Center, target.Center);
                if (dist > BlastRadius)
                    continue;

                int hitDirection = (target.Center.X > Projectile.Center.X) ? 1 : -1;

                NPC.HitInfo hitInfo = target.CalculateHitInfo(
                    finalDamage,
                    hitDirection,
                    isCrit,
                    Projectile.knockBack * (roll == 20 ? 1.5f : 1f),
                    ModContent.GetInstance<ChanceDamageClass>(),
                    false // no damage variation, you already rolled your own
                );

                target.StrikeNPC(hitInfo, fromNet: false, noPlayerInteraction: false);

                if (Main.netMode != NetmodeID.SinglePlayer)
                    NetMessage.SendStrikeNPC(target, hitInfo);

                // Fallback if the meter still doesn't register it:
                Main.player[Projectile.owner].addDPS(hitInfo.Damage);
            }
        }

        // Helper to spawn explosion dusts and shockwave effects
        private void CreateExplosionVisuals()
        {
            // Smoke rings
            for (int i = 0; i < 25; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                Dust smoke = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Smoke, speed.X, speed.Y, 100, default, 1.4f);
                smoke.noGravity = true;
            }

            // Fire sparks
            for (int i = 0; i < 20; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(6f, 6f);
                Dust fire = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.Torch, speed.X, speed.Y, 0, default, 1.8f);
                fire.noGravity = false;
            }

            // Liminal mystic sparkles
            for (int i = 0; i < 15; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(7f, 7f);
                Dust sparkle = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.PurpleCrystalShard, speed.X, speed.Y, 150, default, 1.2f);
                sparkle.noGravity = true;
            }
        }
    }
}
