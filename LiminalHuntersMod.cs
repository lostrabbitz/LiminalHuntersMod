using System.IO;
using LiminalHuntersMod.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LiminalHuntersMod
{
    public enum KylePacketType : byte
    {
        Parry
    }

    public class LiminalHuntersMod : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            KylePacketType type = (KylePacketType)reader.ReadByte();

            switch (type)
            {
                case KylePacketType.Parry:
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
                        packet.Write((byte)KylePacketType.Parry);
                        packet.Write(playerIndex);
                        packet.Write(connected);
                        packet.Send(-1, whoAmI);
                    }
                    break;
            }
        }
    }
}