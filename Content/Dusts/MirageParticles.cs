using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Settings;

namespace LiminalHuntersMod.Content.Dusts
{
    /// <summary>
    /// Shared behaviour: flies outward, fades in its 2nd half, dies after 0.7s.
    /// Draw your PNGs in WHITE/GRAY so the clone color tints them.
    /// </summary>
    public abstract class MirageParticle : ModDust
    {
        protected virtual float Spin => 0.05f;

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = true;
            dust.customData = 0;
            dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            dust.frame = new Rectangle(0, 10 * Main.rand.Next(3), 8, 8); // 3 variants, 8x8 each
        }

        public override bool Update(Dust dust)
        {
            int t = (dust.customData is int i ? i : 0) + 1;
            dust.customData = t;

            if (t >= CloneSettings.ParticleLifeTicks)
            {
                dust.active = false;
                return false;
            }

            dust.position += dust.velocity;
            dust.velocity *= 0.95f;
            dust.rotation += Spin * (dust.velocity.X >= 0f ? 1f : -1f);
            dust.scale *= 0.985f;

            int half = CloneSettings.ParticleLifeTicks / 2;
            dust.alpha = t < half ? 0 : (int)(255f * (t - half) / half);
            return false; // we handled everything
        }

        public override Color? GetAlpha(Dust dust, Color lightColor)
            => dust.color * ((255 - dust.alpha) / 255f);
    }

    public class MirageSmoke : MirageParticle
    {
        protected override float Spin => 0.03f;
    }

    public class MirageStar : MirageParticle
    {
        protected override float Spin => 0.25f;
    }
}
