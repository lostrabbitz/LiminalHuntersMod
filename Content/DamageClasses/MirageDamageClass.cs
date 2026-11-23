using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.DamageClasses
{
    /// <summary>The "mirage" damage class. Benefits from generic (all-class) bonuses only.</summary>
    public class MirageDamageClass : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == Generic)
                return StatInheritanceData.Full;
            return StatInheritanceData.None;
        }

        public override bool UseStandardCritCalcs => true;
    }
}
