using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Content.Dusts;

namespace LiminalHuntersMod.Common.Systems
{
    /// <summary>While a player is Juxtaposed, dust spawned on/around them (running, landing, sliding...) is cancelled.</summary>
    public class JuxtaposeDustMute : ModSystem
    {
        public override void Load()
        {
            On_Dust.NewDust += BlockDust;
        }
 
        private static int BlockDust(On_Dust.orig_NewDust orig, Vector2 Position, int Width, int Height, int Type,
            float SpeedX, float SpeedY, int Alpha, Color newColor, float Scale)
        {
            // our own Mirage particles are exempt (the Juxtapose burst / suction must still show)
            bool ours = Type == ModContent.DustType<MirageStar>() || Type == ModContent.DustType<MirageSmoke>();
            if (!ours && NearJuxtaposedPlayer(Position))
                return Main.maxDust; // vanilla's "no free dust slot" result - callers already handle it
            return orig(Position, Width, Height, Type, SpeedX, SpeedY, Alpha, newColor, Scale);
        }
 
        public static bool NearJuxtaposedPlayer(Vector2 pos)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.GetModPlayer<MiragePlayer>().JuxtaposeTicks <= 0)
                    continue;
                Rectangle r = new Rectangle((int)p.position.X - 24, (int)p.position.Y - 24, p.width + 48, p.height + 48);
                if (r.Contains((int)pos.X, (int)pos.Y))
                    return true;
            }
            return false;
        }
    }
}
