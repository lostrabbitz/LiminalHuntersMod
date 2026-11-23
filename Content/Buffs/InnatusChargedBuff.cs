using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Buffs
{
    public class InnatusChargedBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }
    }
}