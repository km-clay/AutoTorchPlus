# Auto Torch Mod - Design Document

## Concept
Automatically place torches as the player explores to light up the area around them, improving the early-game exploration experience.

## Issues with Existing Implementation
The user tried an existing auto-torch mod and found these problems:

1. **No line-of-sight check** - Torches placed behind walls/blocks where the light doesn't help the player see
2. **No placement feedback** - Torches just appear silently with no sound or visual effect
3. **Missing valid placements** - Doesn't place on trees, wooden beams, or other valid torch surfaces

## Algorithm

### Main Loop (runs every ~30 ticks / 0.5 seconds)
```
1. Get player tile position
2. For each tile in a circular radius around player:
   a. Skip if outside circle (use distance formula: x² + y² > radius²)
   b. Skip if outside world bounds
   c. Skip if player can't see it (line-of-sight check)
   d. Skip if torch already nearby (within spacing distance)
   e. Try to place torch using WorldGen.PlaceTile
   f. Verify placement by checking if tile type is now Torches
   g. Play placement sound and spawn dust effects
   h. Return after placing one torch (only one per check)
```

## Key Implementation Details

### Circle Check (No Trig Required!)
```csharp
// Use Pythagorean theorem in squared form (faster, no sqrt)
if (x*x + y*y > radius*radius) continue;
```

### Line of Sight
```csharp
Vector2 torchPos = new Vector2(tileX * 16 + 8, tileY * 16 + 8);
bool canSee = Collision.CanHitLine(Player.Center, 1, 1, torchPos, 1, 1);
```

### Torch Placement Validation
Use `WorldGen.PlaceTile()` - it handles all the complex validation:
- Solid tiles below (ground, stone, etc.)
- Platforms (solid top tiles)
- Trees (tree trunks)
- Wooden beams and other frame tiles
- Walls behind

**Important:** The return value of `PlaceTile` is unreliable (per docs). Always verify by checking the tile:
```csharp
WorldGen.PlaceTile(tileX, tileY, TileID.Torches, mute: false, forced: false, plr: -1, style: 0);
Tile tile = Main.tile[tileX, tileY];
bool success = tile.HasTile && tile.TileType == TileID.Torches;
```

### Check for Nearby Torches
```csharp
private bool HasTorchNearby(int tileX, int tileY, int spacing) {
    for (int x = -spacing; x <= spacing; x++) {
        for (int y = -spacing; y <= spacing; y++) {
            if (!WorldGen.InWorld(tileX + x, tileY + y)) continue;
            Tile tile = Main.tile[tileX + x, tileY + y];
            if (tile.HasTile && tile.TileType == TileID.Torches) {
                return true;
            }
        }
    }
    return false;
}
```

### Placement Feedback
```csharp
// Sound
SoundEngine.PlaySound(SoundID.Dig, new Vector2(tileX * 16, tileY * 16));

// Visual dust effect
for (int i = 0; i < 3; i++) {
    Dust.NewDust(new Vector2(tileX * 16, tileY * 16), 16, 16, DustID.Torch);
}
```

### Multiplayer Sync
```csharp
if (Main.netMode == NetmodeID.MultiplayerClient) {
    NetMessage.SendTileSquare(-1, tileX, tileY, 1);
}
```

## Configuration Options (Future)
- Enable/disable auto-torch
- Search radius (default: 10 tiles)
- Torch spacing (default: 8 tiles between torches)
- Check frequency (default: every 30 ticks)

## Performance Considerations
- **Throttle checks**: Don't run every frame - every 30 ticks (~0.5 sec) is plenty
- **Circle optimization**: Skip corners of search square (saves ~21% of checks)
- **Early returns**: Use line-of-sight and nearby torch checks to skip expensive tile placement
- **One torch per check**: Only place one torch per update to spread out the work

## Edge Cases
- World bounds checking: Always use `WorldGen.InWorld()` before accessing tiles
- Multiplayer: Sync tile changes with `NetMessage.SendTileSquare()`
- Different torch types: Could be extended to use biome-appropriate torches
- Torch consumption: Could optionally consume torches from player inventory

## Why This Approach Works
1. **Line-of-sight**: Ensures torches only appear where they actually help visibility
2. **Throttling**: Prevents performance issues and feels more natural
3. **Sound/visual feedback**: Makes placement feel responsive and intentional
4. **WorldGen validation**: Handles all vanilla placement rules automatically, including trees and beams
5. **Spacing check**: Prevents torch spam while ensuring good coverage

## Technical Notes
- Tile coordinates are world position / 16
- Tile centers are at (tileX * 16 + 8, tileY * 16 + 8)
- Distance comparisons use squared values to avoid expensive sqrt()
- Dictionary/HashSet lookups are O(1) for caching checks
