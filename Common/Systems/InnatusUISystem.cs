using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using LiminalHuntersMod.Common.UI.ResourceOverlay;

namespace LiminalHuntersMod.Common.Systems
{
    public class InnatusUISystem : ModSystem
    {
        private UserInterface innatusInterface;
        internal InnatusBarUI innatusBarUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                innatusBarUI = new InnatusBarUI();
                innatusBarUI.Activate();
                innatusInterface = new UserInterface();
                innatusInterface.SetState(innatusBarUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            innatusInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1)
            {
                layers.Insert(resourceBarIndex + 1, new LegacyGameInterfaceLayer(
                    "LiminalHuntersMod: Innatus Bar",
                    delegate
                    {
                        if (!Main.gameMenu)
                        {
                            innatusInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}