using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Content.NPCs;
using LiminalHuntersMod.Common.Data;
using LiminalHuntersMod.Content.Buffs;
using LiminalHuntersMod.Common.Utilities;

namespace LiminalHuntersMod.Common.Spawners
{
    public static class CloneSpawner
    {
        /// <summary>Called by the owning client / singleplayer when the held weapon hits something.</summary>
        public static void Request(Player owner)
        {
            if (!owner.HasBuff<MirageClassBuff>())
                return; // mirages only exist while the Mirage buff is active

            CloneData d = CloneData.Build(owner, MirageUtils.ProfileOf(owner.HeldItem), 0);
            if (Main.rand.NextFloat() >= d.CloneChance)
                return;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = ModContent.GetInstance<LiminalHuntersMod>().GetPacket();
                packet.Write((byte)MiragePacket.SpawnClone);
                packet.Send();
            }
            else
            {
                Spawn(owner, d);
            }
        }

        /// <summary>Permanent clones (Doppel) are counted apart from the normal ones.</summary>
        public static int CountActive(int ownerIndex, bool permanent)
        {
            int count = 0;
            int type = ModContent.NPCType<PlayerClone>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == type && n.ModNPC is PlayerClone c && c.OwnerIndex == ownerIndex && c.IsPermanent == permanent)
                    count++;
            }
            return count;
        }

        public static void DespawnPermanent(int ownerIndex)
        {
            int type = ModContent.NPCType<PlayerClone>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.active && n.type == type && n.ModNPC is PlayerClone c && c.OwnerIndex == ownerIndex && c.IsPermanent)
                    c.Despawn();
            }
        }

        /// <summary>Server / singleplayer only. ignoreCap = Juxtapose (works regardless of the weapon's clone limit).</summary>
        public static void Spawn(Player owner, CloneData data, Vector2? at = null, bool ignoreCap = false)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            if (!owner.HasBuff<MirageClassBuff>())
                return; // also covers clones spawned by clones / Juxtapose / the server-side request
            if (!ignoreCap && CountActive(owner.whoAmI, data.Profile.Permanent) >= data.MaxClones)
                return;

            Vector2 pos = at ?? owner.Bottom;
            int i = NPC.NewNPC(owner.GetSource_FromThis(), (int)pos.X, (int)pos.Y, ModContent.NPCType<PlayerClone>());
            if (i < 0 || i >= Main.maxNPCs)
                return;

            NPC npc = Main.npc[i];
            if (npc.ModNPC is PlayerClone clone)
            {
                clone.Setup(owner.whoAmI, data);
                npc.direction = npc.spriteDirection = owner.direction;
            }

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: i);
        }
    }
}
