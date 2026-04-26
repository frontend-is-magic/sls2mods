# MapNodeChanger

MapNodeChanger is a Slay the Spire 2 mod that injects the existing Ancient event `Vakuu` when entering rooms.

It does not modify the map graph, map node type, node icon, or path layout. Instead, it hooks room creation and may replace the room that is about to be entered.

## Default Behavior

- Boss rooms are never replaced.
- The target Ancient defaults to Vakuu and can be changed in the in-game BaseLib mod settings menu.
- Unknown map points have a 66% chance to become the selected Ancient event room.
- Every other non-boss map point has a 6.6% chance to become the selected Ancient event room.
- Natural Ancient rooms are also eligible for the 6.6% roll and can become Vakuu.
- Each injected Vakuu room rerolls the Ancient 3-choice option set for that room.

## Configuration

The mod reads config from `%APPDATA%\SlayTheSpire2\mod_configs\MapNodeChangerConfig.json`.
The enable script creates this file from `MapNodeChangerConfig.json.example` if it does not already exist.
When BaseLib is installed, the in-game mod settings menu is preferred for these values.

```json
{
  "schema_version": 2,
  "enabled": true,
  "seed": 0,
  "ancient_target": "Vakuu",
  "unknown_room_chance": 0.66,
  "other_room_chance": 0.066,
  "replace_natural_ancient": true,
  "log_rolls": true
}
```

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

The packaged mod files are written to `dist\`.
