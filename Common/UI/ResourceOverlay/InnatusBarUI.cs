using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Common.Configs;

namespace LiminalHuntersMod.Common.UI.ResourceOverlay
{
    public class InnatusBarUI : UIState
    {
        private Asset<Texture2D> frameTexture;
        private Asset<Texture2D> fillTexture;
        private Asset<Texture2D> fullFireTexture;

        private int fireAnimTimer;
        private int fireFrameIndex;
        private const int FireFrameCount = 6;
        private const int FireTicksPerFrame = 6;

        public override void OnInitialize()
        {
            if (!Main.dedServ)
            {
                frameTexture = ModContent.Request<Texture2D>("LiminalHuntersMod/Assets/UI/InnatusBarFrame");
                fillTexture = ModContent.Request<Texture2D>("LiminalHuntersMod/Assets/UI/InnatusBarFill");
                fullFireTexture = ModContent.Request<Texture2D>("LiminalHuntersMod/Assets/UI/InnatusFullFire");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var modPlayer = Main.LocalPlayer.GetModPlayer<InnatusPlayer>();
            bool isFull = modPlayer.innatus >= modPlayer.maxInnatus;

            if (isFull)
            {
                fireAnimTimer++;
                if (fireAnimTimer >= FireTicksPerFrame)
                {
                    fireAnimTimer = 0;
                    fireFrameIndex = (fireFrameIndex + 1) % FireFrameCount;
                }
            }
            else
            {
                fireAnimTimer = 0;
                fireFrameIndex = 0;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (Main.dedServ || frameTexture?.Value == null || fillTexture?.Value == null)
                return;

            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<InnatusPlayer>();
            var config = ModContent.GetInstance<InnatusConfig>();

            float maxCap = System.Math.Max(1f, modPlayer.maxInnatus);
            bool isFull = modPlayer.innatus >= maxCap;

            // Bottom-left anchor, stretched horizontally while keeping original sprite assets intact
            float targetWidth = 240f;
            float barHeight = 26f;
            float barX = 24f;
            float barY = Main.screenHeight - 96f;

            Rectangle frameRect = new Rectangle((int)barX, (int)barY, (int)targetWidth, (int)barHeight);

            // 1. Static frame
            spriteBatch.Draw(frameTexture.Value, frameRect, Color.White);

            // 2. Custom-tinted fill based on current Innatus / current maxInnatus
            float clampedInnatus = MathHelper.Clamp(modPlayer.innatus, 0f, maxCap);
            float fillRatio = clampedInnatus / maxCap;

            float widthScale = targetWidth / 140f;
            int innerPadX = (int)(6f * widthScale);
            int innerPadY = 6;
            int maxFillWidth = (int)targetWidth - innerPadX * 2;
            int fillHeight = (int)barHeight - innerPadY * 2;
            int currentFillWidth = (int)(maxFillWidth * fillRatio);

            if (currentFillWidth > 0)
            {
                Rectangle fillDest = new Rectangle((int)barX + innerPadX, (int)barY + innerPadY, currentFillWidth, fillHeight);
                Rectangle fillSource = new Rectangle(0, 0, fillTexture.Value.Width, fillTexture.Value.Height);
                Color fillColor = config?.BarFillColor ?? Color.White;
                spriteBatch.Draw(fillTexture.Value, fillDest, fillSource, fillColor);
            }

            // 3. Animated white fire overlay tinted by config ONLY when bar is full
            if (isFull && fullFireTexture?.Value != null)
            {
                int frameHeight = fullFireTexture.Value.Height / FireFrameCount;
                Rectangle sourceRect = new Rectangle(0, fireFrameIndex * frameHeight, fullFireTexture.Value.Width, frameHeight);
                Color fireColor = config?.FireOverlayColor ?? Color.White;
                spriteBatch.Draw(fullFireTexture.Value, frameRect, sourceRect, fireColor);
            }

            // 4. Hover readout matching vanilla HP / Mana style
            if (frameRect.Contains(Main.MouseScreen.ToPoint()))
            {
                Main.LocalPlayer.mouseInterface = true;
                int displayCurrent = (int)System.Math.Floor(modPlayer.innatus);
                int displayMax = (int)maxCap;
                Main.instance.MouseText($"Innatus: {displayCurrent}/{displayMax}");
            }
        }
    }
}