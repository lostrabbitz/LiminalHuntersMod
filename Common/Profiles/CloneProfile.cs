namespace LiminalHuntersMod.Common.Profiles
{
    /// <summary>Any weapon that summons clones exposes its own profile (the weapon sets the base values).</summary>
    public interface IMirageWeapon
    {
        CloneProfile Profile { get; }
    }

    /// <summary>
    /// Base clone stats of a weapon. Armor / accessories / buffs add BONUSES on top through MiragePlayer
    /// (except on Fixed profiles). Percentages are of the player's current HP / total weapon damage.
    /// </summary>
    public class CloneProfile
    {
        public float DamagePct = 0.30f;
        public float HpPct = 0.30f;
        public float Chance = 0.08f;       // chance per hit to summon a clone
        public float Lifetime = 4f;        // seconds, 0 = unlimited
        public float SpeedBonus = 0f;      // added to the player's run speed
        public int MaxClones = 10;
        public int MaxCascade = 3;         // clone -> child -> grandchild ... depth
        public bool Fixed;                 // count / cascade / chance can't be modified by armor, buffs, accessories
        public bool Permanent;             // lives while the weapon is held (Doppel Lance)
        public CloneProfile Children;      // profile of this clone's children (null = same as this one). Used by Juxtapose clones.
        public static readonly CloneProfile Default = new CloneProfile();

        public static readonly CloneProfile Phantom = new CloneProfile
        {
            DamagePct = 0.60f, Chance = 0.18f, MaxClones = 20, MaxCascade = 4, SpeedBonus = 1f
        };

        public static readonly CloneProfile Doppel = new()
        {
            DamagePct = 1f, HpPct = 1f, Chance = 1f, Lifetime = 0f, MaxClones = 1, MaxCascade = 0,
            Fixed = true, Permanent = true
        };

        /// <summary>Juxtapose clones: this weapon's stats, but 10s, 100% HP, 100% damage and 25% chance (Doppel: default stats, 0% chance).</summary>
        public CloneProfile Juxtapose()
        {
            CloneProfile b = Permanent ? Default : this;
            return new CloneProfile
            {
                DamagePct = 1f, HpPct = 1f, Lifetime = 10f, Chance = Permanent ? 0f : 0.25f,
                SpeedBonus = b.SpeedBonus, MaxClones = b.MaxClones, MaxCascade = b.MaxCascade,
                Children = b   // their children are normal clones of the weapon, not 10s / 100% ones
            };
        }
    }
}