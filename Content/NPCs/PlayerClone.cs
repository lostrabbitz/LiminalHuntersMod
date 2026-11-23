using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Content.Dusts;
using LiminalHuntersMod.Common.Data;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Spawners;
using LiminalHuntersMod.Common.Managers;
using LiminalHuntersMod.Common.Profiles;

namespace LiminalHuntersMod.Content.NPCs
{
    /// <summary>
    /// The Player Clone. Implemented as a friendly NPC so it has real HP and enemies can hit it.
    /// It is drawn by a hidden dummy Player copied from the owner (see EnsureDummy / UpdateDummy).
    ///
    /// ai[0] = swing ticks left   ai[1] = attack cooldown   ai[2] = life ticks left   ai[3] = owner index
    /// </summary>
    public class PlayerClone : ModNPC
    {
        // No PNG needed: we borrow a vanilla texture because ModNPC requires one. It is never drawn.
        public override string Texture => "Terraria/Images/NPC_" + NPCID.Guide;
 
        // ------------------------------------------------------------------
        //  Variables you asked for (snake_case on purpose). Filled from CloneData at spawn.
        // ------------------------------------------------------------------
        public float time_alive;     // seconds before it disappears (0 = never)
        public float clone_chance;   // chance to spawn another clone on a hit (already decayed for this generation)
        public Color clone_color = CloneSettings.DefaultColor;         // yellow by default
        public int dye_item = CloneSettings.DefaultDyeItem;            // dye used on the clone's gear

        private int generation;
        private int damage;
        private bool has_wings;
        public int OwnerIndex => owner_index;
        private int owner_index = 255;
        private int life_ticks;                 // ticks until it expires
        private int swing_ticks;                // swing ticks left
        private int attack_cd;                  // attack cooldown
        private int spawn_life;                 // HP to start with (re-applied on the first AI tick)
        private bool lifeLocked;
        private bool vanishing;                 // true when removed by timeout / owner gone (debug)
        private int lastHitDamage, lifeBeforeHit; // debug
        private int hurtCooldown;               // i-frames, server-side only
        private float damage_reduction;        // owner's endurance, server-side only
        private bool spawnSoundPlayed;
        private CloneProfile profile;           // server-side only: used for child clones / Doppel upkeep
        private int crit_chance;
        private float move_speed;
        private int max_cascade;
        public bool IsPermanent => profile != null && profile.Permanent;
        public void Despawn() => Vanish();      // server / singleplayer
        private int weapon_type;                 // 0 = unarmed -> no weapon drawn, no attacks
        public float size_scale = 1f;        // small random variation per clone
        public float speed_mult = 1f;        // small random variation per clone
        public float attack_speed_mult = 1f; // small random variation per clone (swing + cooldown)

        // Swing / cooldown / hit moment scale with this clone's attack speed (default 12 / 24 / 6 ticks)
        private int SwingLen => Math.Max(4, (int)Math.Round(CloneSettings.SwingTicks / attack_speed_mult));
        private int CooldownLen => Math.Max(SwingLen, (int)Math.Round(CloneSettings.AttackTotalTicks / attack_speed_mult));
        private int HitTick => SwingLen / 2; // damage lands halfway through the swing

        // rendering helpers (client only)
        private Player dummy;
        private int version, builtVersion = -1;
        private bool burstDone;
        private bool grounded;
        private int wingTimer;
 
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NoTownNPCHappiness[Type] = true;
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true };
        }
 
        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.damage = 0;            // contact damage unused; the clone attacks via AI
            NPC.defense = 0;
            NPC.lifeMax = 100;         // overwritten in Setup()
            NPC.friendly = true;       // enemies target friendly / town NPCs...
            NPC.townNPC = true;        // ...(if enemies ignore the clone, check these two flags first)
            NPC.knockBackResist = 0.3f;
            NPC.dontTakeDamageFromHostiles = true; // vanilla hostile-vs-town-NPC damage is disabled; TakeHostileDamage() replaces it
            NPC.noGravity = true;      // gravity is handled manually in AI (needed for wing-flying)
            NPC.value = 0f;
            NPC.dontCountMe = true;
            NPC.HitSound = new SoundStyle($"{nameof(LiminalHuntersMod)}/Assets/Sounds/SFX/NPCs/PhantomClone/PhantomHit")
            {
                Volume = 0.7f,
                PitchVariance = 0.2f,
                MaxInstances = 3
            };
            NPC.DeathSound = new SoundStyle($"{nameof(LiminalHuntersMod)}/Assets/Sounds/SFX/NPCs/PhantomClone/PhantomDeath")
            {
                Volume = 0.85f,
                PitchVariance = 0.2f,
                MaxInstances = 3
            };
        }
 
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) { }
        public override bool CanChat() => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;       // can't kill your own clone
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

 
        // ------------------------------------------------------------------
        //  Setup + netcode
        // ------------------------------------------------------------------
        public void Setup(int ownerIndex, CloneData d)
        {
            generation = d.Generation;
            damage = d.Damage;
            time_alive = d.TimeAlive;
            clone_chance = d.CloneChance;
            clone_color = d.Tint;
            dye_item = d.DyeItem;
            has_wings = d.HasWings;
            weapon_type = d.WeaponType;
            profile = d.Profile;
            crit_chance = d.Crit;
            move_speed = d.MoveSpeed;
            max_cascade = d.MaxCascade;
            NPC.defense = NPC.defDefense = d.Defense;   // owner's armor/accessory defense
            damage_reduction = d.Endurance;             // owner's damage reduction
            size_scale = 1f + Main.rand.NextFloat(-CloneSettings.SizeVariation, CloneSettings.SizeVariation);
            speed_mult = 1f + Main.rand.NextFloat(-CloneSettings.SpeedVariation, CloneSettings.SpeedVariation);
            attack_speed_mult = d.AttackSpeed * (1f + Main.rand.NextFloat(-CloneSettings.AttackSpeedVariation, CloneSettings.AttackSpeedVariation));
            ApplySize();

            life_ticks = time_alive <= 0f ? int.MaxValue : (int)(time_alive * 60f);
            owner_index = ownerIndex;
            spawn_life = Math.Max(1, d.Life);
            NPC.lifeMax = NPC.life = spawn_life; // HP == player's HP when spawned
            version++;
            NPC.netUpdate = true;
        }
 
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(has_wings);
            writer.Write((byte)generation);
            writer.Write(damage);
            writer.Write(time_alive);
            writer.Write(clone_chance);
            writer.Write(clone_color.PackedValue);
            writer.Write(dye_item);
            writer.Write(weapon_type);
            writer.Write(size_scale);
            writer.Write(speed_mult);
            writer.Write(attack_speed_mult);
            writer.Write((byte)owner_index);
            writer.Write(life_ticks);
            writer.Write(swing_ticks);
            writer.Write(attack_cd);
        }
 
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            has_wings = reader.ReadBoolean();
            generation = reader.ReadByte();
            damage = reader.ReadInt32();
            time_alive = reader.ReadSingle();
            clone_chance = reader.ReadSingle();
            clone_color = new Color { PackedValue = reader.ReadUInt32() };
            dye_item = reader.ReadInt32();
            weapon_type = reader.ReadInt32();
            size_scale = reader.ReadSingle();
            speed_mult = reader.ReadSingle();
            attack_speed_mult = reader.ReadSingle();
            owner_index = reader.ReadByte();
            life_ticks = reader.ReadInt32();
            swing_ticks = reader.ReadInt32();
            attack_cd = reader.ReadInt32();
            ApplySize();
            version++;
        }

        // ------------------------------------------------------------------
        //  AI
        // ------------------------------------------------------------------
        public override void AI()
        {
            Player owner = Main.player[owner_index];
 
            // grounded must be read BEFORE gravity is added this tick
            grounded = NPC.velocity.Y == 0f;
 
            if (!spawnSoundPlayed && !Main.dedServ)
            {
                spawnSoundPlayed = true;
                // Resolves to "<ModName>/Assets/Sounds/SFX/NPCs/PhantomClone/PhantomSpawn" (no file extension)
                SoundEngine.PlaySound(new SoundStyle($"{Mod.Name}/Assets/Sounds/SFX/NPCs/PhantomClone/PhantomSpawn") { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            }
 
            // Owner is Juxtaposed: clones are invisible, invulnerable, motionless, and their expiry timer is paused.
            bool frozen = owner.GetModPlayer<MiragePlayer>().JuxtaposeTicks > 0;
            NPC.dontTakeDamage = frozen;
            if (frozen)
            {
                NPC.velocity = Vector2.Zero;
                return;
            }
 
            // ---- runs on every machine (keeps visuals / timers deterministic) ----
            if (swing_ticks == SwingLen && !Main.dedServ)
                SoundEngine.PlaySound(MirageSounds.LanceSwing, NPC.Center);
            if (swing_ticks > 0) swing_ticks--;
            if (swing_ticks == HitTick && !Main.dedServ)
                PlayHitSound();   // clones' hits never go through the lance's OnHitNPC, so the sound is played here
            if (attack_cd > 0) attack_cd--;
            life_ticks--;
 
            if (!has_wings)
                NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.3f, 10f);
 
            // ---- decisions: server / singleplayer only ----
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Authority(owner);
            else
                NPC.spriteDirection = NPC.direction;
 
            if (!Main.dedServ)
                UpdateDummy();
        }
 
        private void Authority(Player owner)
        {
            NPC.DiscourageDespawn(60);
 
            // Safety: make sure nothing between spawn and the first tick reset the HP we copied.
            if (!lifeLocked && spawn_life > 0)
            {
                lifeLocked = true;
                NPC.lifeMax = NPC.life = spawn_life;
            }
 
            if (hurtCooldown > 0)
                hurtCooldown--;
            else
                TakeHostileDamage();
            if (!NPC.active || NPC.life <= 0)
                return;
 
            if (life_ticks <= 0 || !owner.active || owner.dead)
            {
                Vanish();
                return;
            }
 
            Vector2 cursor = owner.GetModPlayer<MiragePlayer>().CursorWorld;
            NPC target = FindTargetNearCursor(cursor);
            Vector2 goal = target != null ? target.Center : cursor;
 
            // face the goal (but not mid-swing)
            if (swing_ticks <= 0 && Math.Abs(goal.X - NPC.Center.X) > 6f)
                NPC.direction = goal.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
 
            bool enemyInReach = EnemyInReach();
            bool stop = target != null ? enemyInReach : Vector2.Distance(NPC.Center, goal) < 24f;
            Move(goal, stop);
 
            // attack: 12-tick swing, then 12 ticks of delay (24 total = 0.40s)
            if (weapon_type > 0 && swing_ticks <= 0 && attack_cd <= 0 && enemyInReach)
            {
                swing_ticks = SwingLen;
                attack_cd = CooldownLen;
                NPC.netUpdate = true;
            }
            else if (swing_ticks == HitTick)
            {
                DoHit(owner);
            }
 
            if (life_ticks % 10 == 0)
                NPC.netUpdate = true;
        }
 
        private void Move(Vector2 goal, bool stop)
        {
            Vector2 toGoal = goal - NPC.Center;

            if (has_wings)
            {
                // Floats freely toward the cursor / target.
                Vector2 desired = stop
                    ? Vector2.Zero
                    : toGoal.SafeNormalize(Vector2.Zero) * Math.Min(move_speed * speed_mult, toGoal.Length() * 0.1f + 1f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, desired, 0.1f);
                if (stop)
                    NPC.velocity.Y += (float)Math.Sin(Main.GameUpdateCount * 0.1f) * 0.05f; // idle hover
            }
            else
            {
                float want = (stop || Math.Abs(toGoal.X) < 12f) ? 0f : Math.Sign(toGoal.X) * move_speed * speed_mult;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, want, 0.2f);

                // hop over blocks / toward higher goals
                if (grounded && want != 0f && (NPC.collideX || (toGoal.Y < -48f && Math.Abs(toGoal.X) < 64f)))
                    NPC.velocity.Y = -8.5f;
            }
        }
 
        private static bool IsValidTarget(NPC n)
        {
            if (!n.active || n.life <= 0 || n.dontTakeDamage)
                return false;
            if (n.type == NPCID.TargetDummy)   // clones are allowed to attack the Target Dummy
                return true;
            return !n.friendly && !n.immortal && n.lifeMax > 5;
        }
 
        private void ApplySize()
        {
            Vector2 bottom = NPC.Bottom;
            NPC.width = Math.Max(8, (int)(20 * size_scale));
            NPC.height = Math.Max(16, (int)(42 * size_scale));
            NPC.Bottom = bottom;
        }
 
        private NPC FindTargetNearCursor(Vector2 cursor)
        {
            NPC best = null;
            float bestDist = CloneSettings.CursorAttackRadius;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!IsValidTarget(n))
                    continue;
                float d = Vector2.Distance(n.Center, cursor);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = n;
                }
            }
            return best;
        }
 
        private Rectangle SwingBox()
        {
            int w = (int)(72 * size_scale), h = (int)(64 * size_scale);
            return new Rectangle((int)(NPC.Center.X + (NPC.direction > 0 ? 0 : -w)), (int)(NPC.Center.Y - h / 2f), w, h);
        }

        /// <summary>Plays the lance hit sound on the first enemy inside the swing box (runs on every client, not the server).</summary>
        private void PlayHitSound()
        {
            Rectangle box = SwingBox();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (IsValidTarget(n) && n.Hitbox.Intersects(box))
                {
                    SoundEngine.PlaySound(MirageSounds.LanceHit, n.Center);
                    return;
                }
            }
        }
 
        private bool EnemyInReach()
        {
            Rectangle box = SwingBox();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (IsValidTarget(n) && n.Hitbox.Intersects(box))
                    return true;
            }
            return false;
        }
 
        private void DoHit(Player owner)
        {
            Rectangle box = SwingBox();
            bool spawnedChild = false;
            var mirage = ModContent.GetInstance<MirageDamageClass>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!IsValidTarget(n) || !n.Hitbox.Intersects(box))
                    continue;

                // credit the owner so kills drop loot
                n.lastInteraction = owner.whoAmI;
                n.playerInteraction[owner.whoAmI] = true;

                int dmg = Math.Max(1, (int)(damage * Main.rand.NextFloat(0.92f, 1.08f)));
                bool crit = Main.rand.Next(100) < crit_chance;
                if (crit) // vanilla already doubles a crit, so scale to the owner's crit multiplier (2 = vanilla)
                    dmg = (int)(dmg * owner.GetModPlayer<MiragePlayer>().CloneCritMult / 2f);
                NPC.HitInfo hit = n.CalculateHitInfo(dmg, NPC.direction, crit, 4f, mirage, false, 0f);
                n.StrikeNPC(hit);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendStrikeNPC(n, hit);
                CreditOwnerDps(owner, hit.Damage);

                // clone_chance: this clone may spawn another clone (max one per swing)
                if (!spawnedChild && profile != null && generation < max_cascade && Main.rand.NextFloat() < clone_chance)
                {
                    spawnedChild = true;
                    CloneSpawner.Spawn(owner, CloneData.Build(owner, profile.Children ?? profile, generation + 1, CloneData.ChildChance(clone_chance, generation + 1)), NPC.Bottom);
                }
            }
        }
 
        /// <summary>
        /// Player-style damage intake: enemy contact + hostile projectiles, one hit per HurtImmunityTicks.
        /// Goes through CalculateHitInfo, so the copied defense and ModifyIncomingHit (damage reduction) apply.
        /// </summary>
        private void TakeHostileDamage()
        {
            Rectangle me = NPC.Hitbox;
 
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || n.friendly || n.damage <= 0 || n.type == NPCID.TargetDummy || !n.Hitbox.Intersects(me))
                    continue;
                ApplyIncomingHit(n.damage, n.Center.X > NPC.Center.X ? -1 : 1);
                return;
            }
 
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || !p.hostile || p.damage <= 0 || !p.Colliding(p.Hitbox, me))
                    continue;
                ApplyIncomingHit(p.damage, p.Center.X > NPC.Center.X ? -1 : 1);
                return;
            }
        }
 
        private void ApplyIncomingHit(int rawDamage, int hitDirection)
        {
            int raw = Math.Max(1, (int)(Main.DamageVar(rawDamage, 0f) * CloneSettings.IncomingDamageMultiplier));
            NPC.HitInfo hit = NPC.CalculateHitInfo(raw, hitDirection, false, 3f, DamageClass.Default, false, 0f);
            lifeBeforeHit = NPC.life;
            if (hit.Damage >= NPC.life)
                NPC.HitSound = null; // lethal blow: let the death sound play alone
            lastHitDamage = hit.Damage;
            NPC.StrikeNPC(hit);
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendStrikeNPC(NPC, hit);
            hurtCooldown = CloneSettings.HurtImmunityTicks;
        }
 
        /// <summary>Makes the owner's DPS meter count the clone's damage as the owner's own.</summary>
        private void CreditOwnerDps(Player owner, int dmg)
        {
            if (dmg <= 0)
                return;
 
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)MiragePacket.DpsCredit);
                packet.Write(dmg);
                packet.Send(owner.whoAmI);
            }
            else if (owner.whoAmI == Main.myPlayer && owner.accDreamCatcher)
            {
                owner.addDPS(dmg);
            }
        }
 
        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // apply the owner's damage reduction on top of the copied defense
            modifiers.FinalDamage *= 1f - damage_reduction;
        }
 
        private void Vanish()
        {
            vanishing = true;
            NPC.HitSound = null; // expiring shouldn't play the "got hit" sound
            NPC.StrikeInstantKill(); // routes through CheckDead / HitEffect so the particles play everywhere
        }
 
        // ------------------------------------------------------------------
        //  Death / disappearing -> yellow smoke + stars
        // ------------------------------------------------------------------
        public override bool CheckDead()
        {
            // Only a real death may get through.
            if (NPC.life > 0)
                return false;
 
            if (CloneSettings.DebugHp && Main.netMode != NetmodeID.Server)
                Main.NewText(vanishing
                    ? $"[Clone] expired (timer {life_ticks}, owner alive: {Main.player[owner_index].active && !Main.player[owner_index].dead})"
                    : $"[Clone] killed by damage: last hit {lastHitDamage}, HP before that hit {lifeBeforeHit}/{NPC.lifeMax}", Color.Orange);
 
            Burst();
            NPC.life = 0;
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
            return false; // skip vanilla death (avoids the "town NPC was slain" chat message)
        }
 
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
                Burst();
        }
 
        private void Burst()
        {
            if (burstDone || Main.dedServ)
                return;
            burstDone = true;
 
            // CheckDead skips vanilla's death handling, so the death sound is played here (runs on every client).
            if (NPC.DeathSound.HasValue)
                SoundEngine.PlaySound(NPC.DeathSound.Value, NPC.Center);
 
            Vector2 c = NPC.Center;
            const int smokeCount = 18, starCount = 12;
 
            for (int i = 0; i < smokeCount; i++)
            {
                float a = MathHelper.TwoPi * i / smokeCount + Main.rand.NextFloat(-0.15f, 0.15f);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
                Dust.NewDustPerfect(c, ModContent.DustType<MirageSmoke>(), v, 0, clone_color, Main.rand.NextFloat(1.2f, 1.8f));
            }
            for (int i = 0; i < starCount; i++)
            {
                float a = MathHelper.TwoPi * i / starCount + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 v = a.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                Dust.NewDustPerfect(c, ModContent.DustType<MirageStar>(), v, 0, clone_color, Main.rand.NextFloat(1f, 1.5f));
            }
        }
 
        // ------------------------------------------------------------------
        //  Rendering: a hidden Player copied from the owner, recolored.
        // ------------------------------------------------------------------
        private void EnsureDummy()
        {
            if (dummy != null && builtVersion == version)
                return;
 
            Player o = Main.player[owner_index];
 
            if (dummy == null)
            {
                dummy = new Player();
                dummy.whoAmI = 255; // not a real player slot
                dummy.active = true;
                dummy.dead = false;
                dummy.GetModPlayer<MiragePlayer>().IsCloneDummy = true;
                dummy.fullRotationOrigin = new Vector2(dummy.width / 2f, dummy.height / 2f);
                dummy.heldProj = -1;
            }
 
            // body
            dummy.Male = o.Male;
            dummy.skinVariant = o.skinVariant;
            dummy.hair = o.hair;
            dummy.hairDye = 0;
 
            // everything fully "yellow" (clone_color)
            Color c = clone_color;
            dummy.skinColor = c;
            dummy.hairColor = c;
            dummy.eyeColor = c;
            dummy.shirtColor = c;
            dummy.underShirtColor = c;
            dummy.pantsColor = c;
            dummy.shoeColor = c;
 
            // same visual armor / accessories as the owner
            dummy.head = o.head; dummy.body = o.body; dummy.legs = o.legs;
            dummy.handon = o.handon; dummy.handoff = o.handoff;
            dummy.back = o.back; dummy.front = o.front; dummy.shoe = o.shoe;
            dummy.waist = o.waist; dummy.shield = o.shield; dummy.neck = o.neck;
            dummy.face = o.face; dummy.balloon = o.balloon;
 
            // dye every gear slot with dye_item so armor/accessories become a single color too
            int shader = GameShaders.Armor.GetShaderIdFromItemId(dye_item);
            dummy.cHead = dummy.cBody = dummy.cLegs = shader;
            dummy.cHandOn = dummy.cHandOff = dummy.cBack = dummy.cFront = shader;
            dummy.cShoe = dummy.cWaist = dummy.cShield = dummy.cNeck = dummy.cFace = shader;
            dummy.cBalloon = dummy.cWings = shader;
 
            // wings: always the same ones, purely visual
            dummy.wings = has_wings ? CloneSettings.WingsId : 0;
 
            // same weapon, visual only (this dummy never runs item code)
            // Only copied if the owner's held item deals mirage damage (decided at spawn -> weapon_type).
            Item held = new Item();
            if (weapon_type > 0)
                held.SetDefaults(weapon_type);
            dummy.inventory[0] = held;
            dummy.selectedItem = 0;
 
            builtVersion = version;
        }
 
        private void UpdateDummy()
        {
            EnsureDummy();
            Player d = dummy;
 
            d.position = NPC.Bottom - new Vector2(d.width / 2f, d.height); // feet stay on the NPC's feet
            d.gfxOffY = NPC.gfxOffY;
            d.direction = NPC.direction == 0 ? 1 : NPC.direction;
            d.velocity = new Vector2(NPC.velocity.X, grounded ? 0f : NPC.velocity.Y);
 
            // swing animation, driven by swing_ticks
            d.lastVisualizedSelectedItem = d.inventory[0]; // the draw layer reads THIS to render the held weapon
            d.itemAnimationMax = SwingLen;
            d.itemAnimation = swing_ticks;
            d.itemTime = d.itemAnimation;
            if (d.itemAnimation > 0)
            {
                float progress = d.itemAnimation / (float)d.itemAnimationMax;
                d.itemRotation = (progress - 0.5f) * -d.direction * 3.5f - d.direction * 0.3f;
                d.itemLocation.X = d.position.X + d.width * 0.5f + (d.HeldItem.width * 0.5f - 4f) * d.direction;
                d.itemLocation.Y = d.position.Y + 24f;
            }
 
            // wing flapping
            if (has_wings && !grounded)
            {
                if (++wingTimer > 5)
                {
                    wingTimer = 0;
                    d.wingFrame = d.wingFrame >= 3 ? 1 : d.wingFrame + 1;
                }
            }
            else
            {
                d.wingFrame = 0;
            }
 
            d.PlayerFrame();
        }
 
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!CloneSettings.DebugHp || Main.dedServ || OwnerJuxtaposed())
                return;
            string text = $"{NPC.life}/{NPC.lifeMax}  DEF {NPC.defense}";
            Utils.DrawBorderString(spriteBatch, text, NPC.Top - screenPos + new Vector2(0f, -16f), Color.White, 0.6f, 0.5f, 1f);
        }
 
        private bool OwnerJuxtaposed()
            => Main.player[owner_index].GetModPlayer<MiragePlayer>().JuxtaposeTicks > 0;
 
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ || OwnerJuxtaposed())
                return false; // invisible during Juxtapose
 
            EnsureDummy();
            // If the clone looks like it floats/sinks after scaling, tweak ScalePivot (0 = scales from top-left, 0.5 = from center).
            const float ScalePivot = 0f;
            float s = size_scale;
            Vector2 drawPos = dummy.position + new Vector2(-dummy.width * 0.5f * (s - 1f), -dummy.height * (1f - ScalePivot) * (s - 1f));
            Main.PlayerRenderer.DrawPlayer(Main.Camera, dummy, drawPos, dummy.fullRotation, dummy.fullRotationOrigin, 0f, s);
            return false; // skip the placeholder NPC sprite
        }
    }
}

