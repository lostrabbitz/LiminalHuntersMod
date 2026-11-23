using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Profiles;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Common.Managers;
using LiminalHuntersMod.Common.Spawners;
using LiminalHuntersMod.Common.Utilities;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    /// <summary>Everything the Phantom / Doppel lances share: swing, right-click Juxtapose, clone summoning.</summary>
    public abstract class MirageLance : ModItem, IMirageWeapon
    {
        public abstract CloneProfile Profile { get; }
        protected abstract int BaseDamage { get; }
        protected abstract int SpriteSize { get; }
        protected abstract float SpriteScale { get; }

        public override void SetDefaults()
        {
            Item.width = SpriteSize;
            Item.height = SpriteSize;
            Item.scale = SpriteScale;    // draws & swings at 2x, same pixel ratio

            Item.damage = BaseDamage;
            Item.DamageType = ModContent.GetInstance<MirageDamageClass>();
            Item.knockBack = 4f;
            Item.crit = 4;

            ApplyLeftClickStats();

            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Green;
        }

        // Left click: 12-tick swing; the other 12 ticks of waiting are enforced by LanceCooldown (NOT reuseDelay:
        // that makes swing-style items swing a second time).
        private void ApplyLeftClickStats()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = CloneSettings.SwingTicks;
            Item.useTime = CloneSettings.SwingTicks;
            Item.noMelee = false;
            Item.autoReuse = true;
            Item.UseSound = null; // the swing sound is played in UseItem (MirageSounds.LanceSwingPlayer)
        }

        // Right click: Juxtapose (hold-up pose, no hitbox)
        private void ApplyRightClickStats()
        {
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.UseSound = null; // MiragePlayer plays its own sounds
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            MiragePlayer mp = player.GetModPlayer<MiragePlayer>();

            if (player.altFunctionUse == 2)
            {
                if (!mp.JuxtaposeReady || !player.GetModPlayer<InnatusPlayer>().CanUseInnatus)
                    return false;
                ApplyRightClickStats();
                return true;
            }

            ApplyLeftClickStats();
            return mp.LanceCooldown <= 0;
        }

        public override bool? UseItem(Player player)
        {
            MiragePlayer mp = player.GetModPlayer<MiragePlayer>();

            if (player.altFunctionUse == 2)
            {
                if (mp.JuxtaposeReady)
                {
                    mp.StartJuxtapose();
                }
                return true;
            }

            // LanceCooldown <= 0 only happens once per swing, so the sound can't double up.
            // The wait scales with attack speed like the swing itself (the clones do the same).
            if (mp.LanceCooldown <= 0)
            {
                mp.LanceCooldown = (int)Math.Round(CloneSettings.AttackTotalTicks / player.GetTotalAttackSpeed(MirageUtils.Mirage));
                SoundEngine.PlaySound(MirageSounds.LanceSwingPlayer, player.Center);
            }
            return true;
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            SoundEngine.PlaySound(MirageSounds.LanceHit, target.Center);
            CloneSpawner.Request(player);
        }
    }
}