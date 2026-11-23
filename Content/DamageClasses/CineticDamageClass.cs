using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.DamageClasses
{
    /// <summary>
    /// Kyle's Class: the "cinetic damage" damage class.
    /// Benefits from generic (all-class) damage/crit/etc. bonuses, but not from melee/ranged/magic-only ones.
    /// </summary>
    public class CineticDamageClass : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == Generic)
                return StatInheritanceData.Full;

            return StatInheritanceData.None;
        }

        public override bool UseStandardCritCalcs => true;

        public override void SetDefaultStats(Player player)
        {
            // Base crit chance for the class (same idea as vanilla classes' +4%).
            player.GetCritChance<CineticDamageClass>() += 4f;
        }
    }
}