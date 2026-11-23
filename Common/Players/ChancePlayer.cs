using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;

namespace LiminalHuntersMod.Common.Players
{
    /// <summary>
    /// Stores custom player variables for the Chance Damage Class.
    /// Can be used by armor sets, accessories, or potions to buff chance mechanics.
    /// </summary>
    public class ChancePlayer : ModPlayer
    {
        // Bonus flat luck or roll modifiers can be added here
        public float chanceDamageMult = 1f;
        public int bonusDiceRoll = 0;
        public bool hasGamblerSetBonus = false;

        public override void ResetEffects()
        {
            chanceDamageMult = 1f;
            bonusDiceRoll = 0;
            hasGamblerSetBonus = false;
        }

        /// <summary>
        /// Convenience method to get Chance Damage scaling on the player.
        /// </summary>
        public float GetTotalChanceDamage()
        {
            return Player.GetTotalDamage<ChanceDamageClass>().ApplyTo(1f);
        }

        /// <summary>
        /// Convenience method to get Chance Crit Chance on the player.
        /// </summary>
        public float GetTotalChanceCrit()
        {
            return Player.GetTotalCritChance<ChanceDamageClass>();
        }
    }
}
