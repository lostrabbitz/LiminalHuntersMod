using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Buffs
{
    public class LiminalSicknessBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // -25% defense via StatModifier
            player.statDefense *= 0.75f;

            // -25% movement speed
            player.moveSpeed -= 0.25f;

            // -25% attack speed (melee/general)
            player.GetAttackSpeed(DamageClass.Generic) -= 0.25f;

            // -50% HP regeneration
            if (player.lifeRegen > 0)
                player.lifeRegen = (int)(player.lifeRegen * 0.5f);
            player.lifeRegenTime = (int)(player.lifeRegenTime * 0.5f);

            // -50% Mana regeneration
            player.manaRegen = (int)(player.manaRegen * 0.5f);
            player.manaRegenDelay *= 1.5f;
        }
    }
}