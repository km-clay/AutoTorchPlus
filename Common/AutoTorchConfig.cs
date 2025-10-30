using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;
using Terraria.ModLoader;

namespace AutoTorchPlus {
	public class AutoTorchConfig : ModConfig {
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Range(2,20)]
		[DefaultValue(typeof(int), "8")]
		public int SearchRadius { get; set; }

		[Range(2,20)]
		[DefaultValue(typeof(int), "12")]
		public int TorchSpacing { get; set; }

		[DefaultValue(typeof(bool), "true")]
		public bool PreferBiomeTorches { get; set; }

		[DefaultValue(typeof(bool), "false")]
		public bool UpgradeNonBiomeTorches { get; set; }

		[DefaultValue(typeof(bool), "true")]
		public bool UseSmartPlacement { get; set; }

		[DefaultValue(typeof(bool), "false")]
		public bool SilentPlacement { get; set; }
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
