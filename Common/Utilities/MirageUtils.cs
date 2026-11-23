using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Profiles;

namespace LiminalHuntersMod.Common.Utilities
{
    public static class MirageUtils
    {
        public static DamageClass Mirage => ModContent.GetInstance<MirageDamageClass>();

        /// <summary>Any weapon that deals mirage damage.</summary>
        public static bool IsMirageWeapon(Item item)
            => !item.IsAir && item.damage > 0 && item.CountsAsClass(Mirage);

        /// <summary>
        /// The player's TOP horizontal speed regardless of what they're doing right now: run / boots speed, and the
        /// wings' flight speed. (accRunSpeed only jumps to the wing speed while you're actually flying, which is why
        /// clones used to be slower unless you spawned them mid-flight.)
        /// </summary>
        public static float TopSpeed(Player p)
        {
            float s = System.Math.Max(p.maxRunSpeed, p.accRunSpeed);
            if (p.wingsLogic > 0)
                s = System.Math.Max(s, ArmorIDs.Wing.Sets.Stats[p.wingsLogic].AccRunSpeedOverride);
            return s;
        }

        /// <summary>The clone profile of whatever is held (non-clone weapons use the defaults).</summary>
        public static CloneProfile ProfileOf(Item item)
            => item.ModItem is IMirageWeapon w ? w.Profile : CloneProfile.Default;

        /// <summary>Enemies the clones / the dash are allowed to hurt (includes the Target Dummy).</summary>
        public static bool IsValidTarget(NPC n)
        {
            if (!n.active || n.life <= 0 || n.dontTakeDamage)
                return false;
            if (n.type == NPCID.TargetDummy)
                return true;
            return !n.friendly && !n.immortal && n.lifeMax > 5;
        }
    }
}
