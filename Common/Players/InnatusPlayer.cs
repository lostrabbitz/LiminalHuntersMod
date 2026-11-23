using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using LiminalHuntersMod.Content.Buffs;
using Microsoft.Xna.Framework;

namespace LiminalHuntersMod.Common.Players
{
    public class InnatusPlayer : ModPlayer
    {
        public const float DefaultStartingMax = 10f;
        public const float DefaultStartingInnatus = 10f;
        public const float AbsoluteMaxInnatus = 200f;
        public const float MinInnatus = -20f;
        private bool reducedThisHit;

        public float maxInnatus = DefaultStartingMax;
        public float innatus = DefaultStartingInnatus;
        public bool isSick;

        private int regenTimer;

        public float SicknessRecoveryThreshold => maxInnatus * 0.25f;

        public bool CanUseInnatus => innatus > 0f && !isSick && !Player.HasBuff<LiminalSicknessBuff>();

        public override void Initialize()
        {
            maxInnatus = DefaultStartingMax;
            innatus = DefaultStartingInnatus;
            isSick = false;
            regenTimer = 0;
        }

        public override void OnRespawn()
        {
            if (maxInnatus < DefaultStartingMax)
                maxInnatus = DefaultStartingMax;

            if (innatus < System.Math.Min(DefaultStartingInnatus, maxInnatus))
                innatus = System.Math.Min(DefaultStartingInnatus, maxInnatus);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["maxInnatus"] = maxInnatus;
            tag["innatus"] = innatus;
            tag["isSick"] = isSick;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("maxInnatus"))
                maxInnatus = MathHelper.Clamp(tag.GetFloat("maxInnatus"), DefaultStartingMax, AbsoluteMaxInnatus);
            else
                maxInnatus = DefaultStartingMax;

            if (tag.ContainsKey("innatus"))
                innatus = MathHelper.Clamp(tag.GetFloat("innatus"), MinInnatus, maxInnatus);
            else
                innatus = System.Math.Min(DefaultStartingInnatus, maxInnatus);

            isSick = tag.ContainsKey("isSick") && tag.GetBool("isSick");
        }

        public override void PostUpdate()
        {
            maxInnatus = MathHelper.Clamp(maxInnatus, DefaultStartingMax, AbsoluteMaxInnatus);
            innatus = MathHelper.Clamp(innatus, MinInnatus, maxInnatus);

            // Trigger Liminal Sickness when empty
            if (innatus <= 0f && !isSick)
            {
                isSick = true;
            }

            // Recovery from Liminal Sickness once Innatus reaches 25% of current maxInnatus
            if (isSick && innatus >= SicknessRecoveryThreshold)
            {
                isSick = false;
            }

            // Regeneration timing (60 ticks = 1 second)
            // Normal: +1 per 0.5s (30 ticks)
            // Liminal Sickness: +1 per 2.0s (120 ticks)
            int interval = isSick ? 120 : 30;

            if (innatus < maxInnatus)
            {
                regenTimer++;
                if (regenTimer >= interval)
                {
                    regenTimer = 0;
                    innatus = MathHelper.Clamp(innatus + 1f, MinInnatus, maxInnatus);
                }
            }
            else
            {
                regenTimer = 0;
            }

            // Apply buffs/debuffs
            if (isSick)
            {
                Player.AddBuff(ModContent.BuffType<LiminalSicknessBuff>(), 2);
            }
            else if (innatus > 0f)
            {
                Player.AddBuff(ModContent.BuffType<InnatusChargedBuff>(), 2);
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            reducedThisHit = !isSick && innatus > 0f;
            if (reducedThisHit)
                modifiers.FinalDamage *= 0.5f;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!reducedThisHit)
                return;

            // info.Damage is already halved, so double it to get the hit before reduction.
            float hpPercent = info.Damage * 2f / Player.statLifeMax2 * 100f;
            float drain = hpPercent <= 15f ? 5f : hpPercent <= 50f ? 10f : 20f;

            innatus = MathHelper.Clamp(innatus - drain, MinInnatus, maxInnatus);
            if (innatus <= 0f)
                isSick = true;
        }

        public void SpendInnatus(float cost)
        {
            innatus = MathHelper.Clamp(innatus - cost, MinInnatus, maxInnatus);
            if (innatus <= 0f)
                isSick = true;
        }
    }
}