using System;
using LiminalHuntersMod.Content.Items.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.Players
{
    /// <summary>
    /// Draws the blade-glow trail. Each particle is the 69x69 glow sprite drawn with the exact
    /// same origin/rotation/flip vanilla uses for the held sword, so it lines up with the blade only.
    /// A colour with alpha 0 renders additively.
    /// </summary>
    public class KylesSaberTrailLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => !drawInfo.drawPlayer.dead;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            KylesSaberPlayer kyle = drawInfo.drawPlayer.GetModPlayer<KylesSaberPlayer>();
            if (kyle.TrailParticles.Count == 0)
                return;

            Texture2D texture = ModContent.Request<Texture2D>(KylesSaber.GlowTexturePath).Value;

            foreach (TrailParticle particle in kyle.TrailParticles)
            {
                float opacity = 1f - particle.Age / KylesSaberPlayer.TrailLifetimeTicks;
                if (opacity <= 0f)
                    continue;

                bool facingRight = particle.Direction == 1;
                Vector2 origin = new Vector2(facingRight ? 0f : texture.Width, texture.Height);
                SpriteEffects effects = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Vector2 position = particle.Position - Main.screenPosition;
                position = new Vector2((int)position.X, (int)position.Y);

                drawInfo.DrawDataCache.Add(new DrawData(
                    texture,
                    position,
                    null,
                    new Color(255, 255, 255, 0) * opacity,
                    particle.Rotation,
                    origin,
                    1f,
                    effects,
                    0));
            }
        }
    }

    /// <summary>Plays the 8-frame (100x100) parry effect over 0.6 s, centered on the player.</summary>
    public class KylesParryEffectLayer : PlayerDrawLayer
    {
        private const int Frames = 8;
        private const int FrameSize = 100;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            => !drawInfo.drawPlayer.dead && drawInfo.drawPlayer.GetModPlayer<KylesSaberPlayer>().ParryEffectTimer > 0;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            KylesSaberPlayer kyle = player.GetModPlayer<KylesSaberPlayer>();

            int elapsed = KylesSaberPlayer.ParryEffectTicks - kyle.ParryEffectTimer;
            int frame = Math.Min(Frames - 1, (int)(elapsed / (float)KylesSaberPlayer.ParryEffectTicks * Frames));

            Texture2D texture = ModContent.Request<Texture2D>(KylesSaberPlayer.ParryEffectTexturePath).Value;
            Rectangle source = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);

            Vector2 position = player.Center + new Vector2(0f, player.gfxOffY) - Main.screenPosition;
            position = new Vector2((int)position.X, (int)position.Y);

            drawInfo.DrawDataCache.Add(new DrawData(
                texture,
                position,
                source,
                Color.White,
                0f,
                new Vector2(FrameSize / 2f, FrameSize / 2f),
                KylesSaberPlayer.ParryEffectScale,
                SpriteEffects.None,
                0));
        }
    }
}