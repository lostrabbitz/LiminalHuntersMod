using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Settings;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Common.Spawners;
using LiminalHuntersMod.Common.Data;
using LiminalHuntersMod.Common.Utilities;

namespace LiminalHuntersMod
{
    public enum ModPacketType : byte
    {
        // Kyle / Parry
        KyleParry,

        // Mirage
        CursorSync,
        SpawnClone,
        DpsCredit,
        JuxtaposeStart,
        JuxtaposeSync
    }

    // Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
    public class LiminalHuntersMod : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            ModPacketType type = (ModPacketType)reader.ReadByte();

            switch (type)
            {
                case ModPacketType.KyleParry:
                {
                    byte playerIndex = reader.ReadByte();
                    bool connected = reader.ReadBoolean();

                    // Never trust the index sent by a client; the server knows who sent it.
                    if (Main.netMode == NetmodeID.Server)
                        playerIndex = (byte)whoAmI;

                    Main.player[playerIndex].GetModPlayer<KylesSaberPlayer>().StartParryVisuals(connected);

                    // Server relays to everyone except the sender.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        ModPacket packet = GetPacket();
                        packet.Write((byte)ModPacketType.KyleParry);
                        packet.Write(playerIndex);
                        packet.Write(connected);
                        packet.Send(-1, whoAmI);
                    }
                    break;
                }

                case ModPacketType.CursorSync:
                {
                    Vector2 pos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    if (Main.netMode == NetmodeID.Server)
                        Main.player[whoAmI].GetModPlayer<MiragePlayer>().CursorWorld = pos;
                    break;
                }

                case ModPacketType.SpawnClone:
                {
                    // Server builds the data itself (never trust client-sent stats).
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Player p = Main.player[whoAmI];
                        CloneSpawner.Spawn(p, CloneData.Build(p, MirageUtils.ProfileOf(p.HeldItem), 0));
                    }
                    break;
                }

                case ModPacketType.DpsCredit:
                {
                    // Server tells the owner's client how much damage their clones just dealt.
                    int dmg = reader.ReadInt32();
                    if (Main.netMode == NetmodeID.MultiplayerClient && Main.LocalPlayer.accDreamCatcher)
                        Main.LocalPlayer.addDPS(dmg);
                    break;
                }

                case ModPacketType.JuxtaposeStart:
                {
                    // A client started Juxtapose: the server runs the timer too (it teleports / spawns the clones),
                    // and tells everybody else so they hide the player and freeze their clones.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Main.player[whoAmI].GetModPlayer<MiragePlayer>().JuxtaposeTicks = CloneSettings.JuxtaposeTicks;

                        ModPacket relay = GetPacket();
                        relay.Write((byte)ModPacketType.JuxtaposeSync);
                        relay.Write((byte)whoAmI);
                        relay.Send(-1, whoAmI);
                    }
                    break;
                }

                case ModPacketType.JuxtaposeSync:
                {
                    int who = reader.ReadByte();
                    if (Main.netMode == NetmodeID.MultiplayerClient && who != Main.myPlayer)
                        Main.player[who].GetModPlayer<MiragePlayer>().JuxtaposeTicks = CloneSettings.JuxtaposeTicks;
                    break;
                }
            }
        }
    }
}