using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Buffs
{
    /// <summary>"Q ability ready". Added/removed by MiragePlayer. Texture: Buffs/MirageDashBuff.png (32x32).</summary>
    public class MirageDashBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
