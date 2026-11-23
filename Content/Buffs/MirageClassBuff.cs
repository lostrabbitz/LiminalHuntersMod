using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Settings;

namespace LiminalHuntersMod.Content.Buffs
{
    /// <summary>Display name "Mirage". Texture: Buffs/MirageClassBuff.png (32x32). Added while the full Mirage armor set is worn.</summary>
    public class MirageClassBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage<MirageDamageClass>().Flat += CloneSettings.MirageBuffFlatDamage;
        }
    }
}
