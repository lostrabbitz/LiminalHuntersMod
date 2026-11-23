using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Common.Profiles;
using LiminalHuntersMod.Common.Utilities;

namespace LiminalHuntersMod.Common.Data
{
    /// <summary>Snapshot of everything a clone needs at the moment it is spawned (profile + player + bonuses).</summary>
    public struct CloneData
    {
        public CloneProfile Profile;   // server-side only (children / upkeep); not synced
        public int Life, Damage, Crit, Defense, DyeItem, Generation, MaxCascade, MaxClones, WeaponType;
        public float TimeAlive, CloneChance, MoveSpeed, AttackSpeed, Endurance;
        public Color Tint;
        public bool HasWings;

        /// <summary>A child's clone chance is half of its parent's.</summary>
        public static float ChildChance(float parentChance, int childGeneration)
            => parentChance * 0.5f;

        public static CloneData Build(Player p, CloneProfile pr, int generation, float? chance = null)
        {
            MiragePlayer mp = p.GetModPlayer<MiragePlayer>();
            Item held = p.HeldItem;
            bool mirage = MirageUtils.IsMirageWeapon(held);     // the clone only holds Mirage weapons
            DamageClass dc = MirageUtils.Mirage;

            // shortcut: Fixed profiles ignore all player bonuses for count / cascade / chance
            int add(int bonus) => pr.Fixed ? 0 : bonus;
            float addF(float bonus) => pr.Fixed ? 0f : bonus;

            int weaponDamage = mirage ? p.GetWeaponDamage(held) : (int)p.GetTotalDamage<MirageDamageClass>().ApplyTo(CloneSettings.BaseDamage);

            return new CloneData
            {
                Profile = pr,
                Life = Math.Max(1, (int)(p.statLife * (pr.HpPct + mp.CloneHpPct))),
                Damage = Math.Max(1, (int)(weaponDamage * (pr.DamagePct + mp.CloneDamagePct))),
                Crit = (mirage ? p.GetWeaponCrit(held) : (int)p.GetTotalCritChance(dc)) + mp.CloneCrit,
                Defense = (int)p.statDefense,
                Endurance = Math.Min(p.endurance, 0.9f),
                TimeAlive = pr.Lifetime <= 0f ? 0f : pr.Lifetime + mp.CloneTimeAlive,
                CloneChance = chance ?? (pr.Chance + addF(mp.CloneChance)),
                MaxCascade = pr.MaxCascade + add(mp.MaxCascade),
                MaxClones = pr.MaxClones + add(mp.MaxClones),
                Generation = generation,
                MoveSpeed = MirageUtils.TopSpeed(p) + pr.SpeedBonus,                    // the player's top speed (+ weapon bonus)
                AttackSpeed = p.GetTotalAttackSpeed(dc),                                // same as the player
                Tint = mp.CloneColor,
                DyeItem = mp.CloneDyeItem,
                HasWings = p.wings > 0 || p.wingsLogic > 0,
                WeaponType = mirage ? held.type : 0
            };
        }
    }
}
