using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria.Audio;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.Systems
{
    /// <summary>
    /// While a player is Juxtaposed, sounds played at their position (footsteps, landing...) are cancelled.
    /// NOTE: this hooks the SoundStyle overload of SoundEngine.PlaySound. If the generated On_ name below doesn't
    /// exist in your tModLoader version, autocomplete on "On_SoundEngine.PlaySound" will list the right one,
    /// or just delete this file (everything else keeps working; you'd only hear footsteps during Juxtapose).
    /// </summary>
    public class JuxtaposeSoundMute : ModSystem
    {
        public override void Load()
        {
            On_SoundEngine.PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback += BlockSound;
        }
 
        private static SlotId BlockSound(On_SoundEngine.orig_PlaySound_refSoundStyle_Nullable1_SoundUpdateCallback orig,
            ref SoundStyle style, Vector2? position, SoundUpdateCallback updateCallback)
        {
            // our own mod's sounds are exempt (Juxtapose vanish / appear etc.)
            bool ours = style.SoundPath != null && style.SoundPath.StartsWith(nameof(LiminalHuntersMod) + "/");
            if (!ours && position.HasValue && JuxtaposeDustMute.NearJuxtaposedPlayer(position.Value))
                return SlotId.Invalid;
            return orig(ref style, position, updateCallback);
        }
    }
}
