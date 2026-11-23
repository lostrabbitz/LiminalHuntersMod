using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.DamageClasses
{
    /// <summary>
    /// Custom damage class for LiminalHuntersMod: Chance Damage.
    /// Weapons with this damage class scale with Chance Damage % and Chance Crit %.
    /// </summary>
    public class ChanceDamageClass : DamageClass
    {
        // Controls stat inheritance from other damage classes.
        // We inherit stats from DamageClass.Generic so standard "all damage" accessories (e.g. Avenger Emblem) boost Chance damage.
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == DamageClass.Generic)
            {
                return StatInheritanceData.Full;
            }

            return StatInheritanceData.None;
        }

        // Controls whether Chance Damage triggers on-hit effects from other damage classes.
        // Typically false so melee flask buffs or magic mana steal don't trigger unless explicitly configured.
        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            return false;
        }

        // Sets default stats for the player if needed
        public override void SetDefaultStats(Player player)
        {
            // Base critical strike chance for Chance class (4% is Terraria standard baseline)
            player.GetCritChance<ChanceDamageClass>() += 4f;
        }

        // Displays "X chance damage" on weapon tooltips
        public override bool ShowStatTooltipLine(Player player, string lineName)
        {
            return true;
        }
    }
}
