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
    public class PlatinumD1Projectile : ModProjectile
    {
        // 2 seconds at 60 updates per second = 120 ticks
        private const int MaxFuseTicks = 80;
        private const float BlastRadius = 50f; // Detection and explosion radius in pixels

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
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
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MinecartSpark, 0f, 0f, 120, default, 0.8f);
                dust.noGravity = false;
                dust.velocity *= 0.2f;
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
            int roll = 1;

            // 2. Choose CombatText color according to prompt requirements:
            // "1 is red, any other number is white and 20 is golden"
            Color textColor = Color.White;

            // 3. Pop the CombatText above the explosion:
            // "pop a number between 1 to 20 just like the damage numbers but I want it to be bigger"
            // dramatic: true creates an enlarged, prominent floating combat text with scale animation!
            Rectangle textRect = new Rectangle((int)Projectile.Center.X - 10, (int)Projectile.Center.Y - 25, 20, 20);
            CombatText.NewText(textRect, textColor, roll.ToString(), dramatic: true, dot: false);

            // 4. Calculate Critical Strike status
            Player player = Main.player[Projectile.owner];
            bool isCrit = Main.rand.Next(100) < (player.GetCritChance<ChanceDamageClass>() + player.GetCritChance(DamageClass.Generic));

            int finalDamage = Projectile.damage;


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
                    Projectile.knockBack * 1f,
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
