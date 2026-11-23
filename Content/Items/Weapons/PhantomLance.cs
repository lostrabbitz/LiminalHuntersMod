using LiminalHuntersMod.Common.Profiles;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    public class PhantomLance : MirageLance
    {
        public override CloneProfile Profile => CloneProfile.Phantom;
        protected override int BaseDamage => 15;
        protected override int SpriteSize => 32;   // Items/PhantomLance.png (32x32)
        protected override float SpriteScale => 2.2f;
    }
}
