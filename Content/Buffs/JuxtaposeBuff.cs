using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Buffs
{
    /// <summary>"Right-click ability ready". Added/removed by MiragePlayer. Texture: Buffs/JuxtaposeBuff.png (32x32).</summary>
    public class JuxtaposeBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
