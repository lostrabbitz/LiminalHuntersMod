using Microsoft.Xna.Framework;
using Terraria.ID;

namespace LiminalHuntersMod.Common.Settings
{
    /// <summary>Every tunable for the Player Clone lives here (or on MiragePlayer for per-player overrides).</summary>
    public static class CloneSettings
    {
        // ---- Defaults (accessories can override these per player via MiragePlayer) ----
        public static readonly Color DefaultColor = Color.Yellow;      // clone / particle color
        public const int DefaultDyeItem = ItemID.PixieDye;             // dye applied to the clone's gear

        // ---- Weapon ----
        public const int BaseDamage = 15;
        public const int SwingTicks = 12;          // 0.20s swing
        public const int AttackTotalTicks = 24;    // 0.20s swing + 0.20s delay = 0.40s

        // ---- Clone behaviour ----
        public static readonly int WingsId = ArmorIDs.Wing.AngelWings;  // the clone always uses these wings
        public const float CursorAttackRadius = 120f;                   // "near the cursor" radius (pixels)
        public const int ParticleLifeTicks = 42;                        // 0.7s
        public const float SizeVariation = 0.08f;                       // +-8% size per clone
        public const float SpeedVariation = 0.10f;                      // +-10% move speed per clone
        public const float AttackSpeedVariation = 0.10f;                // +-10% attack speed per clone

        // ---- Damage the clone TAKES (handled by us, player-style, instead of vanilla town-NPC rules) ----
        public const int HurtImmunityTicks = 40;                        // i-frames after being hit (players get 40)
        public const float IncomingDamageMultiplier = 1f;               // global knob: 0.5 = clones take half damage
        // ---- Mirage buff (full Mirage armor set) ----
        public const float MirageBuffFlatDamage = 4f;                   // +4 mirage damage while the buff is active

        // ---- Liminal Ability: dash (Q) ----  (accessories can modify the live values on MiragePlayer)
        public const float DashCooldownSeconds = 5f;
        public const float DashMaxBlocks = 15f;                         // max distance in tiles
        public const float DashDamageMultiplier = 3f;                   // x held weapon's total damage, to enemies crossed
        public const int DashDurationTicks = 8;                         // how fast the dash itself is
        public const int DashInvulnTicks = 30;                          // 0.5s of invulnerability
        public const int AfterimageLifeTicks = 24;
        public static readonly Color TrailColor = Color.Yellow;
        public const int TrailDyeItem = ItemID.YellowDye;               // dye used on the dash trail's gear

        // ---- Phantom Dodge ----  (accessories can modify the live values on MiragePlayer)
        public const float DodgeChance = 1f;                            // chance to dodge a hit while the dodge is ready
        public const float DodgeCooldownSeconds = 10f;
        public const int DodgeInvulnTicks = 60;                         // 1s of invulnerability after a dodge
        public const int DodgeAfterimageTicks = 20;                     // ~0.33s
        public const float DodgeAfterimageSpeed = 7f;                   // px/tick, shot out to each side

        // ---- Juxtapose (Phantom Lance right-click) ----
        public const int JuxtaposeTicks = 60;                           // 1s invisible
        public const int JuxtaposeCooldownTicks = 900;                  // 900 = 15s, starts when the invisibility ends
        public const int JuxtaposeCloneCount = 2;

        public const bool DebugHp = false;                               // draws "HP/maxHP DEF" above each clone; set false when done testing
    }

    public enum MiragePacket : byte
    {
        CursorSync,
        SpawnClone,
        DpsCredit,
        JuxtaposeStart,   // client -> server
        JuxtaposeSync     // server -> other clients
    }
}
