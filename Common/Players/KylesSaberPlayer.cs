using System.Collections.Generic;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Content.Items.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.Players
{
    public class TrailParticle
    {
        public Vector2 Position;   // world position of the sprite origin (itemLocation)
        public float Rotation;
        public int Direction;
        public float Age;          // in ticks
    }

    public class KylesSaberPlayer : ModPlayer
    {
        // ---------- Parry tuning ----------
        public const float ParryRadius = 160f;              // pixels around the player
        public const int ParryCooldownTicks = 180;          // 3 s
        public const float EnemyDamageMultiplier = 1.5f;    // enemy damage x1.5 + saber damage
        public const float ProjectileDamageMultiplier = 1.8f;
        public const float EnemyKnockback = 8f;
        public const int ParryIFrames = 20;                 // brief invulnerability after a successful parry
        public const bool CooldownOnWhiff = true;           // false = no cooldown if nothing was parried
        public const int ParryEffectTicks = 36;             // 0.6 s total for the 8-frame sheet
        public const string ParryEffectTexturePath = "LiminalHuntersMod/Assets/Textures/ParryEffect"; // 800x100, 8 frames
        public const float ParryEffectScale = 2f;

        // Placeholder sound. To use your own: drop a .wav/.ogg in Assets/Sounds and use
        // new SoundStyle("LiminalHuntersMod/Assets/Sounds/KyleParry")
        public static readonly SoundStyle ParrySound = SoundID.NPCHit4 with { Volume = 0.9f, Pitch = 0.1f };

        // ---------- Trail tuning ----------
        public const int TrailLifetimeTicks = 12;           // 0.2 s: opacity 1 -> 0, then destroyed
        public const int TrailSubsteps = 3;                 // extra interpolated samples per tick for a smooth trail

        public int ParryCooldown;
        public int ParryStanceTimer;
        public int ParryEffectTimer;
        public bool IsParrying => ParryStanceTimer > 0;

        public readonly List<TrailParticle> TrailParticles = new();

        private bool hasLastSample;
        private Vector2 lastLocation;
        private float lastRotation;
        private int lastDirection;

        // =============== PARRY ===============

        /// <summary>Resolves the parry instantly for everything inside the radius. Local player only.</summary>
        public void DoParry(Item saber)
        {
            int saberDamage = Player.GetWeaponDamage(saber);
            Vector2 cursor = Main.MouseWorld; // where the cursor was when the parry fired
            DamageClass kyleClass = ModContent.GetInstance<CineticDamageClass>();
            bool connected = false;

            // ----- Enemies: each one is handled independently -----
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage || npc.immortal || npc.CountsAsACritter)
                    continue;
                if (!InParryRange(npc.Hitbox))
                    continue;

                int damage = (int)(npc.damage * EnemyDamageMultiplier) + saberDamage;
                int hitDirection = npc.Center.X >= Player.Center.X ? 1 : -1;

                Player.ApplyDamageToNPC(npc, damage, EnemyKnockback, hitDirection, false, kyleClass);
                connected = true;
            }

            // ----- Projectiles: each one is handled independently -----
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || !proj.hostile || proj.friendly || proj.damage <= 0)
                    continue;
                if (!InParryRange(proj.Hitbox))
                    continue;

                float speed = System.Math.Max(proj.velocity.Length(), 12f);
                Vector2 direction = (cursor - proj.Center).SafeNormalize(new Vector2(Player.direction, 0f));

                proj.velocity = direction * speed;
                proj.damage = (int)(proj.damage * ProjectileDamageMultiplier);
                proj.hostile = false;
                proj.friendly = true;
                proj.npcProj = false;
                proj.owner = Player.whoAmI;
                proj.DamageType = kyleClass;
                proj.netUpdate = true;
                connected = true;
            }

            ParryCooldown = (connected || CooldownOnWhiff) ? ParryCooldownTicks : 0;

            if (connected)
                Player.SetImmuneTimeForAllTypes(ParryIFrames);

            StartParryVisuals(connected);
            SendParryPacket(connected);
        }

        /// <summary>Stance + (if something was parried) effect and sound. Runs on every client.</summary>
        public void StartParryVisuals(bool connected)
        {
            ParryStanceTimer = KylesSaber.ParryStanceTicks;

            if (connected)
            {
                ParryEffectTimer = ParryEffectTicks;

                if (!Main.dedServ)
                    SoundEngine.PlaySound(ParrySound, Player.Center);
            }
        }

        private void SendParryPacket(bool connected)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)ModPacketType.KyleParry);
            packet.Write((byte)Player.whoAmI);
            packet.Write(connected);
            packet.Send();
        }

        /// <summary>Circle (player center, ParryRadius) vs. the target's hitbox, so large bosses count correctly.</summary>
        private bool InParryRange(Rectangle hitbox)
        {
            Vector2 closest = Vector2.Clamp(Player.Center, hitbox.TopLeft(), hitbox.BottomRight());
            return Vector2.DistanceSquared(Player.Center, closest) <= ParryRadius * ParryRadius;
        }

        // =============== TRAIL ===============

        private void UpdateTrail()
        {
            for (int i = TrailParticles.Count - 1; i >= 0; i--)
            {
                TrailParticles[i].Age += 1f;
                if (TrailParticles[i].Age >= TrailLifetimeTicks)
                    TrailParticles.RemoveAt(i);
            }
        }

        private void EmitTrail()
        {
            bool swinging = !Player.dead
                && Player.itemAnimation > 0
                && ParryStanceTimer <= 0
                && Player.HeldItem.type == ModContent.ItemType<KylesSaber>();

            if (!swinging)
            {
                hasLastSample = false;
                return;
            }

            Vector2 location = Player.itemLocation;
            float rotation = Player.itemRotation;
            int direction = Player.direction;

            if (hasLastSample && lastDirection == direction)
            {
                // Fill the gap between last tick and this tick so fast swings don't leave visible gaps.
                for (int i = 1; i <= TrailSubsteps; i++)
                {
                    float t = i / (float)TrailSubsteps;
                    TrailParticles.Add(new TrailParticle
                    {
                        Position = Vector2.Lerp(lastLocation, location, t),
                        Rotation = MathHelper.Lerp(lastRotation, rotation, t),
                        Direction = direction,
                        Age = (TrailSubsteps - i) / (float)TrailSubsteps
                    });
                }
            }
            else
            {
                TrailParticles.Add(new TrailParticle { Position = location, Rotation = rotation, Direction = direction, Age = 0f });
            }

            hasLastSample = true;
            lastLocation = location;
            lastRotation = rotation;
            lastDirection = direction;
        }

        public override void PostUpdate()
        {
            if (!Main.dedServ)
            {
                UpdateTrail();
                EmitTrail();
            }

            if (ParryCooldown > 0) ParryCooldown--;
            if (ParryStanceTimer > 0) ParryStanceTimer--;
            if (ParryEffectTimer > 0) ParryEffectTimer--;
        }
    }
}