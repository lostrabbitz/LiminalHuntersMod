using LiminalHuntersMod.Common.Profiles;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    /// <summary>One permanent clone: summoned by hitting (100% chance), gone when the lance isn't held (see MiragePlayer.PostUpdate).</summary>
    public class DoppelLance : MirageLance
    {
        public override CloneProfile Profile => CloneProfile.Doppel;
        protected override int BaseDamage => 20;
        protected override int SpriteSize => 22;   // Items/DoppelLance.png (22x22)
        protected override float SpriteScale => 2f;
    }
}