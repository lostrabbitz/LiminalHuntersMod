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
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class LiminalHuntersMod : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            MiragePacket type = (MiragePacket)reader.ReadByte();

            switch (type)
            {
                case MiragePacket.CursorSync:
                {
                    Vector2 pos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                    if (Main.netMode == NetmodeID.Server)
                        Main.player[whoAmI].GetModPlayer<MiragePlayer>().CursorWorld = pos;
                    break;
                }

                case MiragePacket.SpawnClone:
                {
                    // Server builds the data itself (never trust client-sent stats).
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Player p = Main.player[whoAmI];
                        CloneSpawner.Spawn(p, CloneData.Build(p, MirageUtils.ProfileOf(p.HeldItem), 0));
                    }
                    break;
                }

                case MiragePacket.DpsCredit:
                {
                    // Server tells the owner's client how much damage their clones just dealt.
                    int dmg = reader.ReadInt32();
                    if (Main.netMode == NetmodeID.MultiplayerClient && Main.LocalPlayer.accDreamCatcher)
                        Main.LocalPlayer.addDPS(dmg);
                    break;
                }

                case MiragePacket.JuxtaposeStart:
                {
                    // A client started Juxtapose: the server runs the timer too (it teleports / spawns the clones),
                    // and tells everybody else so they hide the player and freeze their clones.
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Main.player[whoAmI].GetModPlayer<MiragePlayer>().JuxtaposeTicks = CloneSettings.JuxtaposeTicks;

                        ModPacket relay = GetPacket();
                        relay.Write((byte)MiragePacket.JuxtaposeSync);
                        relay.Write((byte)whoAmI);
                        relay.Send(-1, whoAmI);
                    }
                    break;
                }

                case MiragePacket.JuxtaposeSync:
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
