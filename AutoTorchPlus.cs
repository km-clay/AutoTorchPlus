using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using AutoTorchPlus.Common.Players;

namespace AutoTorchPlus
{
	public class PacketType {
		public const byte SyncEnabled = 0;
	}
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class AutoTorchPlus : Mod
	{
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			byte packetType = reader.ReadByte();

			switch (packetType) {
				case PacketType.SyncEnabled:
					SyncEnabledStatus(reader);
					break;
			}
		}

		private void SyncEnabledStatus(BinaryReader reader) {
			byte whoIsIt = reader.ReadByte();
			Player player = Main.player[whoIsIt];
			AutoTorchPlayer ATPlayer = player.GetModPlayer<AutoTorchPlayer>();

			ATPlayer.AutoTorchEnabled = reader.ReadBoolean();
		}
	}
}
