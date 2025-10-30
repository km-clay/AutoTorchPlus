using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System;
using System.Numerics;

namespace AutoTorchPlus.Common.Players {
	public class BiomeID {
		public const int Snow = 1;
		public const int Desert = 2;
		public const int Jungle = 3;
		public const int Corrupt = 4;
		public const int Crimson = 5;
		public const int Hallow = 6;
		public const int Dungeon = 7;
		public const int LihzhardTemple = 8;
		public const int Beach = 9;
		public const int Glowshroom = 10;
		public const int Shimmer = 11;
		public const int Forest = 12;
		public static int[] All = {
			Snow,
			Forest,
			Desert,
			Jungle,
			Corrupt,
			Crimson,
			Hallow,
			Dungeon,
			LihzhardTemple,
			Beach,
			Glowshroom,
			Shimmer
		};
	}

	public class AutoTorchPlayer : ModPlayer {
		private bool AutoTorchEnabled = true;
		// Radius 1 is the radius in tiles around the player to search for valid torch spots
		private int AutoTorchRadius1 = 6;
		// Radius 2 is the radius in tiles around potential valid torch spots to look for existing torches
		private int AutoTorchRadius2 = 12;
		private bool MuteTilePlacement = false;
		private bool UseSmartPlacement = true;
		private int TorchCheckInterval = 20;
		private Dictionary<(int,int),bool> tileCache = new();
		private bool UseBiomeTorches = true;
		private bool ReplaceTorches = false;

		private bool PlayerNearPylon = false;

		private const uint TORCH = 1;
		private const uint BLUE_TORCH = 1 << 1;
		private const uint RED_TORCH = 1 << 2;
		private const uint GREEN_TORCH = 1 << 3;
		private const uint PURPLE_TORCH = 1 << 4;
		private const uint WHITE_TORCH = 1 << 5;
		private const uint YELLOW_TORCH = 1 << 6;
		private const uint DEMON_TORCH = 1 << 7;
		private const uint CURSED_TORCH = 1 << 8;
		private const uint ICE_TORCH = 1 << 9;
		private const uint ORANGE_TORCH = 1 << 10;
		private const uint ICHOR_TORCH = 1 << 11;
		private const uint ULTRABRIGHT_TORCH = 1 << 12;
		private const uint BONE_TORCH = 1 << 13;
		private const uint RAINBOW_TORCH = 1 << 14;
		private const uint PINK_TORCH = 1 << 15;
		private const uint DESERT_TORCH = 1 << 16;
		private const uint CORAL_TORCH = 1 << 17;
		private const uint CORRUPT_TORCH = 1 << 18;
		private const uint CRIMSON_TORCH = 1 << 19;
		private const uint HALLOWED_TORCH = 1 << 20;
		private const uint JUNGLE_TORCH = 1 << 21;
		private const uint MUSHROOM_TORCH = 1 << 22;
		private const uint SHIMMER_TORCH = 1 << 23;

		private static bool[] LightSources = [];

		private const uint UNDERWATER_TORCHES = ICHOR_TORCH | CURSED_TORCH | CORAL_TORCH;
		private uint BIOME_TORCHES = 0;
		private uint INV_TORCHES = 0;
		private uint PLAYER_BIOMES = 0;

		private void PopulateLightSources() {
			if (LightSources.Length == 0) {
				LightSources = new bool[Main.tileLighted.Length];
				Array.Copy(Main.tileLighted, LightSources, Main.tileLighted.Length);

				LightSources[TileID.ArgonMoss] = false;
				LightSources[TileID.XenonMoss] = false;
				LightSources[TileID.KryptonMoss] = false;
				LightSources[TileID.LavaMoss] = false;
				LightSources[TileID.VioletMoss] = false;
				LightSources[TileID.RainbowMoss] = false;

				// WHY IS THIS SET TO TRUE IN MAIN.TILELIGHTED????????? RELOGIC???? HELLO???????
				LightSources[TileID.Platforms] = false; // WOODEN PLATFORMS EMIT LIGHT BTW (NO THEY DONT)
			}
		}

		private uint GetInventoryTorches(bool underwater = false) {
			uint torchSet = 0;

			if (underwater) {
				return torchSet & UNDERWATER_TORCHES;
			} else {
				return torchSet;
			}
		}

		private void SetPlayerBiomes() {
			PLAYER_BIOMES |= (Player.ZoneSnow ? (1u << BiomeID.Snow) : 0);
			PLAYER_BIOMES |= (Player.ZoneDesert || Player.ZoneUndergroundDesert ? (1u << BiomeID.Desert) : 0);
			PLAYER_BIOMES |= (Player.ZoneJungle ? (1u << BiomeID.Jungle) : 0);
			PLAYER_BIOMES |= (Player.ZoneCorrupt ? (1u << BiomeID.Corrupt) : 0);
			PLAYER_BIOMES |= (Player.ZoneCrimson ? (1u << BiomeID.Crimson) : 0);
			PLAYER_BIOMES |= (Player.ZoneHallow ? (1u << BiomeID.Hallow) : 0);
			PLAYER_BIOMES |= (Player.ZoneDungeon ? (1u << BiomeID.Dungeon) : 0);
			PLAYER_BIOMES |= (Player.ZoneLihzhardTemple ? (1u << BiomeID.LihzhardTemple) : 0);
			PLAYER_BIOMES |= (Player.ZoneBeach ? (1u << BiomeID.Beach) : 0);
			PLAYER_BIOMES |= (Player.ZoneGlowshroom ? (1u << BiomeID.Glowshroom) : 0);
			PLAYER_BIOMES |= (Player.ZoneShimmer ? (1u << BiomeID.Shimmer) : 0);
			PLAYER_BIOMES |= (Player.ZoneForest ? (1u << BiomeID.Forest) : 0);
		}

		private int[] TorchIDs = [
			ItemID.Torch,
			ItemID.BlueTorch,
			ItemID.RedTorch,
			ItemID.GreenTorch,
			ItemID.PurpleTorch,
			ItemID.WhiteTorch,
			ItemID.YellowTorch,
			ItemID.DemonTorch,
			ItemID.CursedTorch,
			ItemID.IceTorch,
			ItemID.OrangeTorch,
			ItemID.IchorTorch,
			ItemID.UltrabrightTorch,
			ItemID.BoneTorch,
			ItemID.RainbowTorch,
			ItemID.PinkTorch,
			ItemID.DesertTorch,
			ItemID.CoralTorch,
			ItemID.CorruptTorch,
			ItemID.CrimsonTorch,
			ItemID.HallowedTorch,
			ItemID.JungleTorch,
			ItemID.MushroomTorch,
			ItemID.ShimmerTorch,
		];

		public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet) {
			if (AutoTorchHotkey.ToggleAutoTorch.JustPressed) {
				AutoTorchEnabled = !AutoTorchEnabled;
				string status = AutoTorchEnabled ? "enabled" : "disabled";
				Main.NewText($"Auto Torch {status}.", 50, 255, 130);
			}
		}

		private bool TryPlaceTorch(int tileX, int tileY, int usedTorch, int placedTorch) {
			if (!WorldGen.InWorld(tileX, tileY)) return false;
			Tile tile = Main.tile[tileX, tileY];

			// If we got to this point and there is a torch on this tile, that means that
			// the existing torch is the wrong kind, and we want to replace it.
			bool hasTorch = tile.HasTile && tile.TileType == TileID.Torches;
			bool torchIsDifferent = hasTorch && (tile.TileFrameY / 22) != GetTorchStyle(placedTorch); // just in case
			bool hasWire = tile.BlueWire || tile.RedWire || tile.GreenWire || tile.YellowWire;

			// Don't place torches in ways where they will be included or interfere with existing circuit/mechanism logic
			if (hasWire) return false;

			if (hasTorch && ReplaceTorches && torchIsDifferent) {
				// Break the torch. This will drop the previous torch as an item instead of clobbering it out of existence.
				WorldGen.KillTile(tileX, tileY);

				if (Main.netMode == NetmodeID.MultiplayerClient) {
					NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, tileX, tileY);
				}
			}

			if (!hasTorch && (tile.HasTile && !Main.tileCut[tile.TileType])) return false;
			int torchStyle = GetTorchStyle(placedTorch);
			WorldGen.PlaceTile(tileX, tileY, TileID.Torches, mute: MuteTilePlacement, style: torchStyle);

			bool success = tile.HasTile && tile.TileType == TileID.Torches;
			if (success) {

				// Sometimes, the placed torch will inherit the TileFrameX of some decoration it replaced.
				// This can lead to placing unlit torches, which is obviously not what we want.
				// Torches have 6 TileFrameX 'slots' each being 22 pixels
				// If TileFrameX is 66 or above, we subtract 3 slots to move it to the proper tile frame.
				if (tile.TileFrameX >= 66) {
					tile.TileFrameX -= (short)66;
				}

				if (Main.netMode == NetmodeID.MultiplayerClient) {
					NetMessage.SendTileSquare(-1, tileX, tileY, 1);
				}
			}

			return success;
		}

		private bool InBoneTorchBiome() {
			return !Player.ZoneSnow
				&& !Player.ZoneDesert
				&& !Player.ZoneJungle
				&& !Player.ZoneCorrupt
				&& !Player.ZoneCrimson
				&& !Player.ZoneHallow
				&& !Player.ZoneDungeon
				&& !Player.ZoneLihzhardTemple;
		}

		private bool TorchWorksUnderwater(int torchType) {
				return torchType == ItemID.CursedTorch ||
					torchType == ItemID.IchorTorch ||
					torchType == ItemID.CoralTorch;
		}

		private int GetBiomeTorch(bool underwater = false) {
			if (Player.ZoneShimmer && Player.HasItem(ItemID.ShimmerTorch) && !underwater) {
				return ItemID.ShimmerTorch;
			} else if (Player.ZoneSnow && Player.HasItem(ItemID.IceTorch) && !underwater) {
				return ItemID.IceTorch;
			} else if (Player.ZoneCrimson && (Player.HasItem(ItemID.CrimsonTorch) || Player.HasItem(ItemID.IchorTorch))) {
				if (Player.HasItem(ItemID.CrimsonTorch) && !underwater) {
					return ItemID.CrimsonTorch;
				} else if (Player.HasItem(ItemID.IchorTorch)) {
					return ItemID.IchorTorch;
				} else {
					return -1;
				}
			} else if (Player.ZoneCorrupt && (Player.HasItem(ItemID.CorruptTorch) || Player.HasItem(ItemID.CursedTorch))) {
				if (Player.HasItem(ItemID.CorruptTorch) && !underwater) {
					return ItemID.CorruptTorch;
				} else if (Player.HasItem(ItemID.CursedTorch)) {
					return ItemID.CursedTorch;
				} else {
					return -1;
				}
			} else if (Player.ZoneHallow && Player.HasItem(ItemID.HallowedTorch) && !underwater) {
				return ItemID.HallowedTorch;
			} else if (Player.ZoneJungle && Player.HasItem(ItemID.JungleTorch) && !underwater) {
				return ItemID.JungleTorch;
			} else if ((Player.ZoneDesert || Player.ZoneUndergroundDesert) && Player.HasItem(ItemID.DesertTorch) && !underwater) {
				return ItemID.DesertTorch;
			} else if (Player.ZoneBeach && Player.HasItem(ItemID.CoralTorch)) {
				return ItemID.CoralTorch;
			} else if (Player.ZoneGlowshroom && Player.HasItem(ItemID.MushroomTorch) && !underwater) {
				return ItemID.MushroomTorch;
			} else if (InBoneTorchBiome() && Player.HasItem(ItemID.BoneTorch) && !underwater) {
				return ItemID.BoneTorch;
			} else if (Player.HasItem(ItemID.Torch) && !underwater) {
				return ItemID.Torch;
			} else {
				return -1;
			}
		}

		private uint GetBiomeTorchSetForBiome(int biomeID, bool underwater = false) {
			// Get place-able torches regardless of if the player has them
			uint torchSet = 0;
			switch (biomeID) {
				case BiomeID.Shimmer:
					torchSet = SHIMMER_TORCH;
					break;
				case BiomeID.Snow:
					torchSet = ICE_TORCH;
					break;
				case BiomeID.Crimson:
					torchSet = CRIMSON_TORCH | ICHOR_TORCH;
					break;
				case BiomeID.Corrupt:
					torchSet = CORRUPT_TORCH | CURSED_TORCH;
					break;
				case BiomeID.Hallow:
					torchSet = HALLOWED_TORCH;
					break;
				case BiomeID.Jungle:
					torchSet = JUNGLE_TORCH;
					break;
				case BiomeID.Desert:
					torchSet = DESERT_TORCH;
					break;
				case BiomeID.Beach:
					torchSet = CORAL_TORCH;
					break;
				case BiomeID.Glowshroom:
					torchSet = MUSHROOM_TORCH;
					break;
				case BiomeID.Forest:
					torchSet = BONE_TORCH;
					break;
				default:
					torchSet = TORCH;
					break;
			}

			torchSet |= TORCH; // Always allow regular torch

			if (underwater) {
				torchSet &= UNDERWATER_TORCHES;
			}
			return torchSet;
		}

		private int HighestSetBit(uint v) {
			if (v == 0) return -1;
			return 31 - BitOperations.LeadingZeroCount(v);
		}


		private int GetTorchStyle(int torchType) {
			return torchType switch {
				ItemID.Torch => 0,
				ItemID.BlueTorch => 1,
				ItemID.RedTorch => 2,
				ItemID.GreenTorch => 3,
				ItemID.PurpleTorch => 4,
				ItemID.WhiteTorch => 5,
				ItemID.YellowTorch => 6,
				ItemID.DemonTorch => 7,
				ItemID.CursedTorch => 8,
				ItemID.IceTorch => 9,
				ItemID.OrangeTorch => 10,
				ItemID.IchorTorch => 11,
				ItemID.UltrabrightTorch => 12,
				ItemID.BoneTorch => 13,
				ItemID.RainbowTorch => 14,
				ItemID.PinkTorch => 15,
				ItemID.DesertTorch => 16,
				ItemID.CoralTorch => 17,
				ItemID.CorruptTorch => 18,
				ItemID.CrimsonTorch => 19,
				ItemID.HallowedTorch => 20,
				ItemID.JungleTorch => 21,
				ItemID.MushroomTorch => 22,
				ItemID.ShimmerTorch => 23,
				_ => 0
			};
		}
		private int TorchTypeFromStyle(int torchStyle) {
			return torchStyle switch {
				0 => ItemID.Torch,
				1 => ItemID.BlueTorch,
				2 => ItemID.RedTorch,
				3 => ItemID.GreenTorch,
				4 => ItemID.PurpleTorch,
				5 => ItemID.WhiteTorch,
				6 => ItemID.YellowTorch,
				7 => ItemID.DemonTorch,
				8 => ItemID.CursedTorch,
				9 => ItemID.IceTorch,
				10 => ItemID.OrangeTorch,
				11 => ItemID.IchorTorch,
				12 => ItemID.UltrabrightTorch,
				13 => ItemID.BoneTorch,
				14 => ItemID.RainbowTorch,
				15 => ItemID.PinkTorch,
				16 => ItemID.DesertTorch,
				17 => ItemID.CoralTorch,
				18 => ItemID.CorruptTorch,
				19 => ItemID.CrimsonTorch,
				20 => ItemID.HallowedTorch,
				21 => ItemID.JungleTorch,
				22 => ItemID.MushroomTorch,
				23 => ItemID.ShimmerTorch,
				_ => 0
			};
		}

		private bool HasLineOfSight(int x1, int y1, int x2, int y2, int maxDepth = 3) {
			int dx = Math.Abs(x2 - x1);
			int dy = Math.Abs(y2 - y1);
			int sx = x1 < x2 ? 1 : -1;
			int sy = y1 < y2 ? 1 : -1;
			int err = dx - dy;

			int blocksHit = 0;

			while (true) {
				if (x1 == x2 && y1 == y2) return true;

				if (!WorldGen.InWorld(x1, y1)) return false;

				Tile tile = Main.tile[x1, y1];
				if (tile.HasTile && tile.TileType != TileID.Cobweb && !Main.tileCut[tile.TileType] && Main.tileBlockLight[tile.TileType]) blocksHit++;
				if (blocksHit >= maxDepth) return false;

				int e2 = 2 * err;
				if (e2 > -dy) { err -= dy; x1 += sx; }
				if (e2 < dx) { err += dx; y1 += sy; }
			}
		}

		private (int consumed, int placed) GetTorches(bool underwater) {
			uint available = INV_TORCHES;

			if (TorchGodEnabled() && (available & TORCH) != 0) {
				int consumed = ItemID.Torch;

				uint placeSet = BIOME_TORCHES;
				if (underwater) placeSet &= UNDERWATER_TORCHES;

				if (underwater && placeSet == 0) {
					// This biome doesn't have any torches that work underwater.
					// Let's do this as a last ditch effort to find a torch to place
					placeSet = (INV_TORCHES & UNDERWATER_TORCHES);
					int fallbackPlacedStyle = HighestSetBit(placeSet);
					if (fallbackPlacedStyle == -1) {
						return (-1, -1);
					}
					int fallbackPlaced = TorchTypeFromStyle(fallbackPlacedStyle);
					return (fallbackPlaced, fallbackPlaced);
				}
				else {
					int placedStyle = HighestSetBit(placeSet);
					int placed = placedStyle != -1 ? TorchTypeFromStyle(placedStyle) : ItemID.Torch;

					return (consumed, placed);
				}
			}

			if (UseBiomeTorches) {
				available &= BIOME_TORCHES;
			}
			if (underwater) {
				available &= UNDERWATER_TORCHES;
			}

			if (underwater && available == 0) {
				// We don't have any biome specific torches, and we are underwater.
				// Let's do this as a last ditch effort to find a torch to place
				available = (INV_TORCHES & UNDERWATER_TORCHES);
			}

			int style = HighestSetBit(available);
			if (style == -1) return (-1, -1);
			int torchType = TorchTypeFromStyle(style);
			return (torchType, torchType);
		}

		private void CheckAroundPlayer() {
			int playerX = (int)(Player.position.X / 16);
			int playerY = (int)(Player.position.Y / 16);

			for (int x = -AutoTorchRadius1; x <= AutoTorchRadius1; x++) {
				for (int y = -AutoTorchRadius1; y <= AutoTorchRadius1; y++) {
					if ((x*x) + (y*y) > (AutoTorchRadius1*AutoTorchRadius1)) continue;
					int x2 = playerX + x;
					int y2 = playerY + y;
					Tile candidate = Main.tile[x2, y2];

					uint rightTorches = BIOME_TORCHES;
					bool playerInForest = ((PLAYER_BIOMES & (1u << BiomeID.Forest)) != 0);
					if (!playerInForest) rightTorches &= ~TORCH;

					bool candidateHasWrongTorch = candidate.HasTile
						&& candidate.TileType == TileID.Torches
						&& UseBiomeTorches
						&& ReplaceTorches
						&& ((rightTorches & (1u << (candidate.TileFrameY / 22))) == 0);

					bool noLightsNearby = !CheckAroundTile(x2, y2);

					if (candidateHasWrongTorch || !noLightsNearby) {
					}

					if (candidateHasWrongTorch || noLightsNearby) {
						int depth = 1;
						if (candidateHasWrongTorch) depth++;
						if (!HasLineOfSight(playerX, playerY, (playerX + x), (playerY + y), maxDepth: depth)) {
							continue;
						}
						Tile tile = Main.tile[x2, y2];
						bool isUnderwater = tile.LiquidAmount > 0;

						var (usedTorch, placedTorch) = GetTorches(isUnderwater);
						if (usedTorch == -1 || placedTorch == -1) continue;

						if (TryPlaceTorch(x2, y2, usedTorch, placedTorch)) {
							Player.ConsumeItem(usedTorch);
							return;
						}
					}
				}
			}
		}

		// The function for our "is there an existing light source" heuristics
		private bool HasLightSource(int x, int y) {
			// if there is no tile, just return
			if (!Main.tile[x,y].HasTile) return false;

			// We *always* check for torches
			bool hasTorch = Main.tile[x,y].TileType == TileID.Torches;
			if (hasTorch) {
				return true;
			}
			// the only cuttable lightsources in the game are *very* dim
			if (Main.tileCut[Main.tile[x,y].TileType]) {
				return false;
			}

			// if the player is underground and not near a pylon, we don't care about light sources
			// however, if we are in a glowing mushroom biome, those places are generally well lit, so we can reasonably check for light sources
			// to conserve torches. In this case, the light sources would be the glowing mushroom grass
			// The dungeon also has some naturally occuring light sources
			bool playerIsUnderground = !Player.ZoneOverworldHeight;
			bool playerNotNearPylon = !PlayerNearPylon;
			bool playerNotInGlowshroom = !Player.ZoneGlowshroom;
			if (playerIsUnderground && playerNotNearPylon && playerNotInGlowshroom) {
				return false;
			}

			// if we are here, the tile's location is under the following conditions:
			// 1. On the surface (any light sources here will be player-made)
			// 2. Underground, but near a pylon, which means we are near player-made structures
			// Now we care about checking for any lightsources (candles, chandeliers, etc), not just torches
			bool hasLightSource = LightSources[Main.tile[x,y].TileType];
			return hasLightSource;
		}

		private int GetSightlineDepth() {
			if (Player.ZoneUnderworldHeight) {
				// The underworld has a lot of 1 block thick walls in it's obsidian towers
				// And a lot of naturally occurring torches. It prevents us from adequately
				// covering the obsidian towers if we use the default line of sight depth.
				// With depth = 1, the LoS will not penetrate the walls of the towers
				return 1;
			} else {
				return 3;
			}
		}

		private bool CheckAroundTile(int tileX, int tileY) {

			for (int x = -AutoTorchRadius2; x <= AutoTorchRadius2; x++) {
				for (int y = -AutoTorchRadius2; y <= AutoTorchRadius2; y++) {
					int x2 = tileX + x;
					int y2 = tileY + y;
					if (!WorldGen.InWorld(x2, y2)) continue;
					bool hasLightSource = false;

					if (tileCache.ContainsKey((x2,y2)) && tileCache[(x2,y2)]) {
						hasLightSource = true;
					} else {
						hasLightSource = HasLightSource(x2,y2);
					}

					if (hasLightSource) {
						tileCache[(x2,y2)] = true;
						if (!UseSmartPlacement) return true;

						// If the player is on the surface, we skip the line-of-sight check between torches
						// This is because the surface is largely flat, and the slight differences in height
						// can cause the line of sight check to spam torches more than necessary
						if (Player.ZoneOverworldHeight) return true;

						int depth = GetSightlineDepth();
						// If the existing torch can see this tile, it's already being lit up
						if (HasLineOfSight(tileX, tileY, x2, y2, maxDepth: depth)) return true;
					}
				}
			}
			return false;
		}

		private bool TorchGodEnabled() {
			return (Player.unlockedBiomeTorches && Player.UsingBiomeTorches);
		}

		public override void PreUpdate() {
			if (!AutoTorchEnabled) return;
			if (LightSources.Length == 0) PopulateLightSources();

			INV_TORCHES = 0;
			BIOME_TORCHES = 1;
			PLAYER_BIOMES = 0;

			SetPlayerBiomes();

			for (int i = 0; i < Player.inventory.Length; i++) {
				if (Player.inventory[i] == null) continue;
				Item slotItem = Player.inventory[i];
				int shift = GetTorchStyle(slotItem.type);
				if (shift >= 0 && shift <= 23) {
					INV_TORCHES |= (1u << shift);
				}
			}

			for (int i = 0; i < BiomeID.All.Length; i++) {
				if ((PLAYER_BIOMES & (1u << BiomeID.All[i])) == 0) continue;
				uint biomeTorches = GetBiomeTorchSetForBiome(BiomeID.All[i]);
				BIOME_TORCHES |= biomeTorches;
			}
		}

		public override void PostUpdate() {
			if (!AutoTorchEnabled) return;
			if (Player.dead) return;
			uint updateNum = Main.GameUpdateCount;
			if (updateNum % TorchCheckInterval != 0) return;

			PlayerNearPylon = Terraria.GameContent.TeleportPylonsSystem.IsPlayerNearAPylon(Player);

			var config = ModContent.GetInstance<AutoTorchConfig>();
			AutoTorchRadius1 = config.SearchRadius;
			AutoTorchRadius2 = config.TorchSpacing;
			MuteTilePlacement = config.SilentPlacement;
			UseSmartPlacement = config.UseSmartPlacement;
			UseBiomeTorches = config.PreferBiomeTorches;
			ReplaceTorches = config.UpgradeNonBiomeTorches;

			tileCache.Clear();
			CheckAroundPlayer();
		}
	}
}
