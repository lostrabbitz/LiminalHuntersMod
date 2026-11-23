using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Buffs
{
    /// <summary>"Phantom Dodge" - dodge-ready indicator. Added/removed by MiragePlayer. Texture: Buffs/PhantomDodgeBuff.png (32x32).</summary>
    public class PhantomDodgeBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
