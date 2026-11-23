using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Content.Buffs;
using LiminalHuntersMod.Content.Dusts;
using LiminalHuntersMod.Content.NPCs;
using LiminalHuntersMod.Common.Data;
using LiminalHuntersMod.Common.Systems;
using LiminalHuntersMod.Common.Utilities;
using LiminalHuntersMod.Common.Spawners;
using LiminalHuntersMod.Content.Items.Weapons;
using LiminalHuntersMod.Common.Managers;
using LiminalHuntersMod.Common.Profiles;

namespace LiminalHuntersMod.Common.Players
{
    public struct Afterimage
    {
        public Vector2 Position;
        public int Direction;
        public Rectangle BodyFrame;
        public Rectangle LegFrame;
        public Vector2 Velocity;
        public int Age;
        public int Life;
    }

        public class MiragePlayer : ModPlayer
    {
        // ==================================================================
        //  Clone values. Reset every tick -> accessories just set them in UpdateAccessory:
        //     var mp = player.GetModPlayer<MiragePlayer>();
        //     mp.CloneColor = Color.Cyan; mp.CloneDyeItem = ItemID.CyanDye; mp.CloneTimeAlive += 2f;
        // ==================================================================
        public Color CloneColor = CloneSettings.DefaultColor;
        public int CloneDyeItem = CloneSettings.DefaultDyeItem;
        // Everything below is a BONUS added to the weapon's own clone profile (CloneProfile), reset every tick:
        public float CloneTimeAlive;      // + seconds of lifetime
        public float CloneChance;         // + clone chance (0.05 = +5%)
        public int MaxCascade;            // + cascade depth
        public float CloneHpPct;          // + share of the player's current HP   (0.1 = +10 points)
        public float CloneDamagePct;      // + share of the player's weapon damage
        public int CloneCrit;             // + crit chance %
        public float CloneCritMult = 2f;  // crit damage multiplier (2 = vanilla)

        // ==================================================================
        //  Dash values. Same idea: accessories can change them, e.g.
        //     mp.DashCooldownSeconds -= 1f;  mp.DashMaxBlocks += 5f;  mp.DashDamageMultiplier += 1f;
        // ==================================================================
        public float DashCooldownSeconds = CloneSettings.DashCooldownSeconds;
        public float DashMaxBlocks = CloneSettings.DashMaxBlocks;
        public float DashDamageMultiplier = CloneSettings.DashDamageMultiplier;
        public float DodgeChance = CloneSettings.DodgeChance;
        public float DodgeCooldownSeconds = CloneSettings.DodgeCooldownSeconds;
        public int DodgeCooldown;             // ticks
        public int DashBonusDamage;                                   // flat damage added on top of the dash's normal damage
        public int MaxClones;                                         // + max clones at once

 
        /// <summary>Cursor in world coordinates. Synced to the server so clone AI can use it.</summary>
        public Vector2 CursorWorld;
 
        /// <summary>True only on hidden Players used to *render* clones / afterimages.</summary>
        public bool IsCloneDummy;
 
        /// <summary>Ticks until the Phantom Lance can be swung again.</summary>
        public int LanceCooldown;
 
        // ---- dash runtime ----
        public int DashCooldown;               // ticks
        private int dashTicks;
        private int dashInvuln;
        private Vector2 dashStart, dashEnd;
        private int dashDamage;
        private readonly HashSet<int> dashHit = new HashSet<int>();
        public List<Afterimage> Afterimages = new List<Afterimage>();
        private Player trailDummy;
 
        // ---- Juxtapose runtime ----
        public int JuxtaposeTicks;             // > 0 while invisible (synced to every client)
        public int JuxtaposeCooldown;          // ticks (owner only)
        public bool JuxtaposeReady => JuxtaposeCooldown <= 0 && JuxtaposeTicks <= 0 && HasMirageBuff; // needs the armor
 
        private bool wasJuxtaposed;
        private bool wasDoppel;
        private Vector2 lastSent;
        private int syncTimer;

        public bool HasMirageBuff => Player.HasBuff<MirageClassBuff>();
 
        public override void ResetEffects()
        {
            CloneColor = CloneSettings.DefaultColor;
            CloneDyeItem = CloneSettings.DefaultDyeItem;
            CloneTimeAlive = 0f;
            CloneChance = 0f;
            MaxCascade = 0;
            CloneHpPct = 0f;
            CloneDamagePct = 0f;
            CloneCrit = 0;
            CloneCritMult = 2f;

            DashCooldownSeconds = CloneSettings.DashCooldownSeconds;
            DashMaxBlocks = CloneSettings.DashMaxBlocks;
            DashDamageMultiplier = CloneSettings.DashDamageMultiplier;
            DashBonusDamage = 0;
            DodgeChance = CloneSettings.DodgeChance;
            DodgeCooldownSeconds = CloneSettings.DodgeCooldownSeconds;
            MaxClones = 0;
        }
 
        // ------------------------------------------------------------------
        //  Input
        // ------------------------------------------------------------------
        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
 
            if (MirageKeybinds.LiminalAbility != null && MirageKeybinds.LiminalAbility.JustPressed)
                TryDash();
        }
 
        // ------------------------------------------------------------------
        //  Dash (Liminal Ability)
        // ------------------------------------------------------------------
        private void TryDash()
        {
            if (Player.dead || Player.CCed || dashTicks > 0 || DashCooldown > 0 || JuxtaposeTicks > 0)
                return;
            if (!HasMirageBuff || !MirageUtils.IsMirageWeapon(Player.HeldItem))
                return;
 
            Vector2 from = Player.Center;
            Vector2 delta = Main.MouseWorld - from;
            float max = DashMaxBlocks * 16f;
            if (delta.Length() > max)
                delta = Vector2.Normalize(delta) * max;
            if (delta.Length() < 16f)
                return;
 
            Vector2 destCenter = from + delta;
 
            // Destination must have a free 2x3 tile area (32x48 px), otherwise no dash and no cooldown.
            if (Collision.SolidCollision(destCenter - new Vector2(16f, 24f), 32, 48))
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                return;
            }

            if (!TrySpendInnatus(3f))
                return;

            dashStart = Player.position;
            dashEnd = destCenter - new Vector2(Player.width / 2f, Player.height / 2f);
            dashTicks = CloneSettings.DashDurationTicks;
            dashInvuln = CloneSettings.DashInvulnTicks;
            dashDamage = (int)(Player.GetWeaponDamage(Player.HeldItem) * DashDamageMultiplier) + DashBonusDamage;
            dashHit.Clear();
            DashCooldown = (int)(DashCooldownSeconds * 60f);
            Player.direction = delta.X >= 0f ? 1 : -1;
 
            Player.ClearBuff(ModContent.BuffType<MirageDashBuff>()); // "ready" buff disappears while on cooldown
            SoundEngine.PlaySound(SoundID.Item8, Player.Center);
        }
 
        public override void PreUpdateMovement()
        {
            if (dashTicks <= 0)
                return;
 
            Vector2 prevCenter = Player.Center;
 
            int step = CloneSettings.DashDurationTicks - dashTicks + 1;
            float p = step / (float)CloneSettings.DashDurationTicks;
            Player.position = Vector2.Lerp(dashStart, dashEnd, p);   // ignores tiles on purpose (passes through walls)
            Player.velocity = Vector2.Zero;
            Player.fallStart = (int)(Player.position.Y / 16f);      // no fall damage afterwards
 
            DashStrike(prevCenter, Player.Center);
 
            // yellow mirage stars left behind
            for (int i = 0; i < 2; i++)
            {
                Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(14f, 20f),
                    ModContent.DustType<MirageStar>(), Main.rand.NextVector2Circular(1.5f, 1.5f), 0, CloneColor, Main.rand.NextFloat(1f, 1.5f));
            }
 
            // afterimage of the player (drawn in yellow by AfterimageSystem)
            Afterimages.Add(new Afterimage
            {
                Position = Player.position,
                Direction = Player.direction,
                BodyFrame = Player.bodyFrame,
                LegFrame = Player.legFrame,
                Age = 0,
                Life = CloneSettings.AfterimageLifeTicks
            });
 
            dashTicks--;
            if (dashTicks == 0)
                Player.velocity = Vector2.Zero;
        }
 
        private void DashStrike(Vector2 a, Vector2 b)
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
 
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!MirageUtils.IsValidTarget(n) || dashHit.Contains(i))
                    continue;
 
                float cp = 0f;
                if (!Collision.CheckAABBvLineCollision(n.position, n.Size, a, b, Player.width, ref cp))
                    continue;
 
                dashHit.Add(i);
                int dir = b.X >= a.X ? 1 : -1;
                NPC.HitInfo hit = n.CalculateHitInfo(dashDamage, dir, false, 6f, MirageUtils.Mirage, false, Player.luck);
                n.StrikeNPC(hit);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendStrikeNPC(n, hit);
                if (Player.accDreamCatcher)
                    Player.addDPS(hit.Damage);
            }
        }
 
        /// <summary>Shared "ability ready again" effect (dash + Juxtapose): same idea as the "mana is full" effect.</summary>
        private void ReadyEffects()
        {
            SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
            for (int i = 0; i < 8; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(2f, 2f);
                Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height) * 0.5f,
                    ModContent.DustType<MirageStar>(), v, 0, CloneColor, Main.rand.NextFloat(1f, 1.4f));
            }
        }

 
        // ------------------------------------------------------------------
        //  Juxtapose (Phantom Lance right-click)
        // ------------------------------------------------------------------
        /// <summary>Called by the owning client when the lance is right-clicked.</summary>
        public void StartJuxtapose()
        {
            if (!TrySpendInnatus(10f))
                return;

            JuxtaposeTicks = CloneSettings.JuxtaposeTicks;
            Player.ClearBuff(ModContent.BuffType<JuxtaposeBuff>());
 
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MiragePacket.JuxtaposeStart);
                packet.Send();
            }
        }

        /// <summary>Mirage stars burst OUT of the player + vanish sound.</summary>
        public void PlayJuxtaposeDisappearEffects()
        {
            if (Main.dedServ)
                return;
 
            SoundEngine.PlaySound(MirageSounds.JuxtaposeDisappear, Player.Center);
            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float a = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(3f, 6f);
                Dust.NewDustPerfect(Player.Center, ModContent.DustType<MirageStar>(), v, 0, CloneColor, Main.rand.NextFloat(1f, 1.5f));
            }
        }
 
        /// <summary>Mirage stars get SUCKED INTO the player + reappear sound.</summary>
        public void PlayJuxtaposeAppearEffects()
        {
            if (Main.dedServ)
                return;
 
            SoundEngine.PlaySound(MirageSounds.JuxtaposeAppear, Player.Center);
            const int count = 16;
            for (int i = 0; i < count; i++)
            {
                float a = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 offset = a.ToRotationVector2() * Main.rand.NextFloat(60f, 100f);
                // particles slow down by 5% per tick, so offset/16 makes them arrive around the player's center
                Dust.NewDustPerfect(Player.Center + offset, ModContent.DustType<MirageStar>(), -offset / 16f, 0, CloneColor, Main.rand.NextFloat(1f, 1.5f));
            }
        }
 
        private void OnJuxtaposeEnd()
        {
            if (Player.whoAmI == Main.myPlayer)
                JuxtaposeCooldown = CloneSettings.JuxtaposeCooldownTicks;
 
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return; // the server / singleplayer handles the clones
 
            // all active clones teleport to the player (their timers resume by themselves)
            int type = ModContent.NPCType<PlayerClone>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == type && n.ModNPC is PlayerClone c && c.OwnerIndex == Player.whoAmI)
                {
                    n.Bottom = Player.Bottom;
                    n.velocity = Vector2.Zero;
                    n.netUpdate = true;
                }
            }
 
            // 2 extra clones with the held weapon's profile (see CloneProfile.Juxtapose); they ignore the clone limit.
            // shortcut: uses the weapon held when Juxtapose ENDS, not when it started; store it at the start if that matters
            CloneProfile jp = MirageUtils.ProfileOf(Player.HeldItem).Juxtapose();
            for (int k = 0; k < CloneSettings.JuxtaposeCloneCount; k++)
                CloneSpawner.Spawn(Player, CloneData.Build(Player, jp, 0), null, true);
        }

        // ------------------------------------------------------------------
        //  Spend Innatus
        // ------------------------------------------------------------------

        private bool TrySpendInnatus(float cost)
        {
            var innatus = Player.GetModPlayer<InnatusPlayer>();
            if (!innatus.CanUseInnatus)
                return false;

            innatus.SpendInnatus(cost);
            return true;
        }
 
        // ------------------------------------------------------------------
        //  Per-tick upkeep
        // ------------------------------------------------------------------
        public override void PostUpdateEquips()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            // The three "ready" buffs show with the full armor + a held Mirage weapon (and only while the ability is ready)
            bool armed = HasMirageBuff && MirageUtils.IsMirageWeapon(Player.HeldItem);

            if (armed && DashCooldown <= 0 && dashTicks <= 0 && JuxtaposeTicks <= 0)
                Player.AddBuff(ModContent.BuffType<MirageDashBuff>(), 2);

            if (armed && DodgeCooldown <= 0)
                Player.AddBuff(ModContent.BuffType<PhantomDodgeBuff>(), 2);

            // Juxtapose is a lance ability (Phantom AND Doppel)
            if (armed && Player.HeldItem.ModItem is MirageLance && JuxtaposeReady)
                Player.AddBuff(ModContent.BuffType<JuxtaposeBuff>(), 2);
        }
 
        public override void PostUpdate()
        {
            if (LanceCooldown > 0)
                LanceCooldown--;
 
            if (dashInvuln > 0)
                dashInvuln--;
 
            // afterimage aging
            for (int i = Afterimages.Count - 1; i >= 0; i--)
            {
                Afterimage a = Afterimages[i];
                a.Age++;
                a.Position += a.Velocity;
                a.Velocity *= 0.9f;
                if (a.Age >= a.Life)
                    Afterimages.RemoveAt(i);
                else
                    Afterimages[i] = a;
            }
 
            // Juxtapose timers run on every machine (the server needs them to teleport / spawn clones)
            if (JuxtaposeTicks > 0)
            {
                JuxtaposeTicks--;
                if (JuxtaposeTicks == 0)
                    OnJuxtaposeEnd();
            }
            else if (JuxtaposeCooldown > 0)
            {
                JuxtaposeCooldown--;
                if (JuxtaposeCooldown == 0 && Player.whoAmI == Main.myPlayer && HasMirageBuff)
                    ReadyEffects();   // same sound + particles as the dash being ready
            }
 
            if (Player.whoAmI != Main.myPlayer)
                return;
 
            if (DashCooldown > 0)
            {
                DashCooldown--;
                if (DashCooldown == 0 && HasMirageBuff)
                    ReadyEffects();
            }
 
            // Disappear / reappear effects follow the Juxtapose state itself, on EVERY machine and from the same place
            // (PostUpdate) - so they always play, no matter who/what started the Juxtapose.
            bool juxtaposed = JuxtaposeTicks > 0;
            if (juxtaposed && !wasJuxtaposed)
                PlayJuxtaposeDisappearEffects();
            else if (!juxtaposed && wasJuxtaposed)
                PlayJuxtaposeAppearEffects();
            wasJuxtaposed = juxtaposed;

            // Doppel Lance: its clone only appears from hitting (100% chance) and goes away when the lance isn't held
            bool doppel = MirageUtils.ProfileOf(Player.HeldItem).Permanent;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!doppel && wasDoppel)
                    CloneSpawner.DespawnPermanent(Player.whoAmI);
            }
            wasDoppel = doppel;

            if (Player.whoAmI != Main.myPlayer)
                return;

            if (DodgeCooldown > 0)
                DodgeCooldown--;
 
            if (DashCooldown > 0)
            {
                DashCooldown--;
                if (DashCooldown == 0 && HasMirageBuff)
                    ReadyEffects();
            }

            CursorWorld = Main.MouseWorld;
 
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                syncTimer++;
                if (syncTimer >= 6 && Vector2.DistanceSquared(CursorWorld, lastSent) > 16f * 16f)
                {
                    syncTimer = 0;
                    lastSent = CursorWorld;
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)MiragePacket.CursorSync);
                    packet.Write(CursorWorld.X);
                    packet.Write(CursorWorld.Y);
                    packet.Send();
                }
            }
        }
 
        // ------------------------------------------------------------------
        //  Invulnerability, no item use, invisibility
        // ------------------------------------------------------------------
        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
            => dashInvuln > 0 || JuxtaposeTicks > 0;

        /// <summary>Phantom Dodge: dodges an incoming hit (chance, then cooldown) with two yellow afterimages shot to the sides.</summary>
        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            if (DodgeCooldown > 0 || !HasMirageBuff || !MirageUtils.IsMirageWeapon(Player.HeldItem))
                return false;
            if (Main.rand.NextFloat() >= DodgeChance)
                return false;
            if (!TrySpendInnatus(2f))
                return false;

            DodgeCooldown = (int)(DodgeCooldownSeconds * 60f);
            // A dodge only cancels THIS hit. Without i-frames the same enemy hits again on the next tick,
            // so the dodge also grants invulnerability (ImmuneTo reads dashInvuln).
            dashInvuln = System.Math.Max(dashInvuln, CloneSettings.DodgeInvulnTicks);
            Player.ClearBuff(ModContent.BuffType<PhantomDodgeBuff>());
            SoundEngine.PlaySound(MirageSounds.Dodge, Player.Center);

            for (int side = -1; side <= 1; side += 2)
                Afterimages.Add(new Afterimage
                {
                    Position = Player.position,
                    Direction = Player.direction,
                    BodyFrame = Player.bodyFrame,
                    LegFrame = Player.legFrame,
                    Velocity = new Vector2(side * CloneSettings.DodgeAfterimageSpeed, 0f),
                    Age = 0,
                    Life = CloneSettings.DodgeAfterimageTicks
                });
            return true;
        }
 
        public override bool CanUseItem(Item item)
            => JuxtaposeTicks <= 0;
 
        public override void HideDrawLayers(PlayerDrawSet drawInfo)
        {
            if (JuxtaposeTicks > 0)
            {
                foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.Layers)
                    layer.Hide();
            }
        }
 
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            // Make eyes fully yellow too (only for the render-dummies).
            if (!IsCloneDummy)
                return;
            drawInfo.colorEyeWhites = drawInfo.colorBodySkin;
            drawInfo.colorEyes = drawInfo.colorBodySkin;
        }
 
        // ------------------------------------------------------------------
        //  Afterimage drawing (called by AfterimageSystem)
        // ------------------------------------------------------------------
        public void DrawAfterimages()
        {
            if (Afterimages.Count == 0)
                return;
 
            trailDummy ??= DummyAppearance.Create();
            Player d = trailDummy;
            DummyAppearance.Apply(d, Player, CloneSettings.TrailColor, CloneSettings.TrailDyeItem);
 
            foreach (Afterimage a in Afterimages)
            {
                float t = a.Age / (float)a.Life;   // 0 -> fresh, 1 -> gone
                d.position = a.Position;
                d.direction = a.Direction;
                d.bodyFrame = a.BodyFrame;
                d.legFrame = a.LegFrame;
                d.velocity = Vector2.Zero;
                Main.PlayerRenderer.DrawPlayer(Main.Camera, d, d.position, 0f, d.fullRotationOrigin, 0.4f + 0.55f * t, 1f);
            }
        }
    }
}
