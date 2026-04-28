# AllSnakebite

AllSnakebite is a Slay the Spire 2 mod inspired by the game's `Snakebite` card naming. It applies a snakebite rule set to every card.

## Default Behavior

- Every card behaves as if it has `Retain`.
- Every card title is displayed with `蛇咬` appended once.
- Attack card damage is converted into the same amount of `Poison` on the original targets.
- Non-card damage, monster damage, relic damage, and non-attack card damage are not converted.
- The in-game BaseLib mod settings menu exposes an enabled toggle.

## Install

Use the repository-level Windows 11 script:

```text
..\..\manage-mods-win11.bat
```

The script installs the built files from `mods\AllSnakebite\dist` into the game's `mods\AllSnakebite` folder.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\AllSnakebite\build.ps1
```

The packaged mod files are written to `dist\`.
