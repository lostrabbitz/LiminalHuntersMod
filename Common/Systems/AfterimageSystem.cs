using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;

namespace LiminalHuntersMod.Common.Systems
{
    /// <summary>Draws the yellow dash trail (behind players / NPCs).</summary>
    public class AfterimageSystem : ModSystem
    {
        public override void PostDrawTiles()
        {
            if (Main.dedServ || Main.gameMenu)
                return;

            bool any = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && p.GetModPlayer<MiragePlayer>().Afterimages.Count > 0)
                {
                    any = true;
                    break;
                }
            }
            if (!any)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.outOfRange)
                    p.GetModPlayer<MiragePlayer>().DrawAfterimages();
            }

            Main.spriteBatch.End();
        }
    }
}
