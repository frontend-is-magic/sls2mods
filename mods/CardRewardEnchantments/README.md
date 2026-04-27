# CardRewardEnchantments

CardRewardEnchantments is a Slay the Spire 2 mod that adds one random allowed enchantment to eligible card rewards.

## Install

Players should install or remove this mod with the repository-level Windows 11 script:

```text
..\..\manage-mods-win11.bat
```

The script asks for the Slay the Spire 2 folder with a folder picker, then installs the built files from `mods\CardRewardEnchantments\dist` into the game's `mods\CardRewardEnchantments` folder.

## Default Behavior

- The mod is enabled by default.
- Card rewards have a 100% enchantment roll chance by default.
- Cards that already have an enchantment are skipped.
- The in-game BaseLib mod settings menu exposes the enable toggle, roll chance, log toggle, and known enchantment blacklist checkboxes.
- The reserved precise enchantment option exists only in config code for a later phase and is not shown in the in-game menu.

## Configuration

The mod reads config from `%APPDATA%\SlayTheSpire2\mod_configs\CardRewardEnchantmentsConfig.json`.
If the file does not exist, the mod creates it on load.
When BaseLib is installed, the in-game mod settings menu is preferred for current values.

```json
{
  "schema_version": 1,
  "enabled": true,
  "enchant_chance": 1.0,
  "blacklisted_keywords": [],
  "log_rolls": true
}
```

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\CardRewardEnchantments\build.ps1
```

The packaged mod files are written to `dist\`.
