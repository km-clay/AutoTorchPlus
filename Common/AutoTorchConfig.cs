using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;

namespace AutoTorchPlus {
	public class AutoTorchConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header($"$Mods.AutoTorchPlus.Config.Headers.SearchBehavior")]
		[Range(2,20)]
		[DefaultValue(typeof(int), "10")]
		public int SearchRadius { get; set; }

		[Range(2,20)]
		[DefaultValue(typeof(int), "18")]
		public int TorchSpacing { get; set; }

		[Header($"$Mods.AutoTorchPlus.Config.Headers.BiomeTorches")]
		[DefaultValue(typeof(bool), "true")]
		public bool PreferBiomeTorches { get; set; }

		[DefaultValue(typeof(bool), "false")]
		public bool UpgradeNonBiomeTorches { get; set; }

		[Header($"$Mods.AutoTorchPlus.Config.Headers.TorchPlacement")]
		[DefaultValue(typeof(bool), "true")]
		public bool UseSmartPlacement { get; set; }

		[DefaultValue(typeof(bool), "false")]
		public bool SilentPlacement { get; set; }

		[Header($"$Mods.AutoTorchPlus.Config.Headers.Advanced")]
		[DefaultValue(typeof(bool), "false")]
		public bool ShowDebugInfo { get; set; }
	}

	public class AutoTorchHotkey : ModSystem {
		public static ModKeybind ToggleAutoTorch { get; private set; }

		public override void Load() {
			ToggleAutoTorch = KeybindLoader.RegisterKeybind(Mod, "ToggleAutoTorch", "Y");
		}
		public override void Unload() {
			ToggleAutoTorch = null;
		}
	}
}
