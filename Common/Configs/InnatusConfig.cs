using System.ComponentModel;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.Config;

namespace LiminalHuntersMod.Common.Configs
{
    public class InnatusConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(typeof(Color), "130, 220, 255, 255")]
        public Color BarFillColor { get; set; } = new Color(130, 220, 255, 255);

        [DefaultValue(typeof(Color), "255, 160, 50, 255")]
        public Color FireOverlayColor { get; set; } = new Color(255, 160, 50, 255);
    }
}