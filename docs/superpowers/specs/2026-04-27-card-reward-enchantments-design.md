# Card Reward Enchantments Design

## Goal

Create a new Slay the Spire 2 mod, `CardRewardEnchantments`, that can automatically add a random enchantment to card rewards.

The first release should:

- Add a configurable chance to enchant each reward card. The default is `100%`.
- Add an in-game menu blacklist for enchantment keywords. Blacklisted keywords are excluded from automatic card reward enchantments.
- Prefer checkbox-style blacklist controls in the in-game mod menu.
- Dynamically collect available enchantment keywords from game data when possible.
- Fall back to an internal default keyword list when dynamic collection fails or returns no usable keywords.
- Reserve design space for a more precise enchantment selector, but do not expose or implement that selector in the first release.

## Non-Goals

- Do not enchant shop cards, deck cards, event-granted cards, or other non-reward cards unless they go through the same card reward generation path.
- Do not remove or rewrite enchantments that the base game or another mod already added.
- Do not add a second enchantment to cards that already have one in the first release.
- Do not expose the precise enchantment selector in the first release.
- Do not refactor existing mods beyond shared utilities that are needed by this mod.

## Recommended Approach

Use a Harmony patch after card rewards are generated. The patch passes the generated reward cards to a service that applies the configured chance, filters enchantment keywords through the blacklist, and adds one random allowed enchantment to each eligible card.

This keeps the visible reward screen aligned with the final card state. It also avoids the higher risk of patching card creation globally, which could affect shops, events, debug cards, or existing deck cards.

## Project Layout

Add a new mod directory:

```text
mods/CardRewardEnchantments/
```

Expected first-release files:

```text
mods/CardRewardEnchantments/CardRewardEnchantments.cs
mods/CardRewardEnchantments/CardRewardEnchantments.csproj
mods/CardRewardEnchantments/CardRewardEnchantments.json
mods/CardRewardEnchantments/CardRewardEnchantmentsConfig.json.example
mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfig.cs
mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantConfigMenu.cs
mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantInstaller.cs
mods/CardRewardEnchantments/Features/CardRewards/CardRewardEnchantService.cs
mods/CardRewardEnchantments/Features/CardRewards/EnchantmentKeywordCatalog.cs
mods/CardRewardEnchantments/Features/CardRewards/CardRewardAdapter.cs
mods/CardRewardEnchantments/build.ps1
```

Add shared utilities:

```text
utils/Config/ModConfigLoader.cs
utils/Config/ModConfigMenuRegistrar.cs
utils/Randoming/DeterministicSeed.cs
```

Optional, if it reduces repeated patching boilerplate without hiding target-specific details:

```text
utils/Harmony/HarmonyPatchInstaller.cs
```

## Shared Utilities

### `ModConfigLoader`

`ModConfigLoader` handles shared JSON fallback behavior:

- Resolve `%APPDATA%/SlayTheSpire2/mod_configs`.
- Load a mod-specific JSON config file.
- Create a default config when the file does not exist.
- Validate `schema_version`.
- Catch malformed JSON and incompatible schema versions.
- Normalize config values before returning them.
- Save default config with indented JSON.

The utility should accept the mod id, file name, supported schema version, default factory, normalize callback, and log callback. It should not know about card enchantment behavior.

### `ModConfigMenuRegistrar`

`ModConfigMenuRegistrar` handles shared BaseLib menu registration:

- Call `ModConfigRegistry.Register(modId, menu)`.
- Catch registration failures, including BaseLib-related exceptions.
- Log success or failure consistently.
- Return `true` when the in-game menu is available, `false` when the caller should use JSON fallback.

New mods should use this helper instead of hand-writing menu registration in each mod entry point.

### `DeterministicSeed`

Move the deterministic string-to-`uint` seed helper out of `AncientOptionRerollService` into `utils/Randoming`.

The card reward enchantment mod can use stable seed material such as run seed, reward identity, card index, and mod name. Existing Ancient option reroll code can migrate to the same helper later, but that migration is not required for the first release unless it is low-risk.

### `HarmonyPatchInstaller`

This is optional. If used, keep it thin:

- Resolve a target method.
- Resolve a prefix, postfix, or transpiler method.
- Patch with Harmony.
- Log failure when a method cannot be found.

Do not hide target-specific signatures or game-specific assumptions inside the helper.

## Mod Components

### Mod Entry Point

`CardRewardEnchantments` owns startup wiring:

1. Load JSON fallback config with `ModConfigLoader`.
2. Try to register `CardRewardEnchantConfigMenu` with `ModConfigMenuRegistrar`.
3. Build a runtime config provider:
   - If menu registration succeeds, read from menu static properties.
   - If menu registration fails, use the loaded JSON fallback config.
4. Build `EnchantmentKeywordCatalog`.
5. Build `CardRewardEnchantService`.
6. Install `CardRewardEnchantInstaller`.
7. Log startup status.

The entry point should stay thin. Game reward object access belongs in installer or adapter classes, and enchantment selection belongs in the service.

### Runtime Config

`CardRewardEnchantConfig` contains:

- `schema_version`, default `1`.
- `enabled`, default `true`.
- `enchant_chance`, default `1.0`.
- `blacklisted_keywords`, default empty.
- `log_rolls`, default `true`.

`Normalize()` clamps probability into `0..1`, removes duplicate blacklist entries, trims invalid keyword strings, and keeps unknown keywords as strings so the config remains forward-compatible.

### In-Game Menu

`CardRewardEnchantConfigMenu : SimpleModConfig` contains:

- `Enabled` boolean toggle.
- `EnchantChancePercent` slider from `0.0` to `100.0`, default `100.0`.
- Checkbox-style boolean properties for known enchantment keywords.

BaseLib menu properties are compile-time static properties, so first release should define checkbox properties for the known fallback keywords. Dynamic keyword discovery can inform runtime filtering and logging, but it should not require dynamic UI generation.

If runtime discovery finds a keyword that has no checkbox property, it remains eligible unless it is listed in JSON fallback blacklist. The log should mention newly discovered keywords so future versions can add them to the checkbox menu.

The precise enchantment selector is only reserved in the design. It should not appear as a hidden disabled control in the first release menu.

### Enchantment Keyword Catalog

`EnchantmentKeywordCatalog` tries to collect available enchantment keywords from game data or model APIs. Because the exact public API may vary across game versions, the catalog should isolate reflection or model lookup in one place.

Catalog behavior:

- Try dynamic collection first.
- Normalize keyword ids or names into stable string keys.
- Remove null, empty, duplicate, or malformed entries.
- Sort output for stable logs and tests.
- Fall back to an internal default keyword list when dynamic collection fails or returns no entries.
- Log whether dynamic or fallback keywords are being used.

First release keeps this catalog inside the new mod. It can move to `utils/Enchantments` later when a second enchantment-related mod needs it.

### Card Reward Enchant Service

`CardRewardEnchantService` applies the mod behavior to a reward card list.

For each card:

1. Skip if the mod is disabled.
2. Skip if the card already has an enchantment.
3. Roll against `enchant_chance`.
4. Build the allowed keyword set from catalog keywords minus config blacklist.
5. Skip if the allowed set is empty.
6. Pick one keyword with the provided RNG.
7. Ask `CardRewardAdapter` to apply that enchantment to the card.
8. Log success or a recoverable per-card failure.

The service should be testable without real game reward objects by using small interfaces or adapter methods for "has enchantment" and "apply enchantment".

### Card Reward Installer And Adapter

`CardRewardEnchantInstaller` locates and patches the card reward generation method. The preferred target is the method that runs after a card reward list has been fully generated but before the reward screen is shown.

`CardRewardAdapter` keeps game-specific access in one place:

- Extract reward cards from the patched object or return value.
- Detect whether a card already has an enchantment.
- Apply a selected enchantment keyword to a card.
- Resolve an RNG source or seed material when the patched method does not provide one directly.

If the hook target cannot be found, the installer logs the failure and leaves the mod loaded but inactive.

## Runtime Flow

```text
ModLoaded
  -> Load JSON fallback config
  -> Try register BaseLib menu
  -> Build runtime config provider
  -> Build enchantment keyword catalog
  -> Install card reward Harmony patch

Card rewards generated
  -> Adapter extracts reward cards
  -> Service reads runtime config
  -> Service filters catalog keywords by blacklist
  -> Service rolls once per card
  -> Adapter applies one random allowed enchantment
```

## Error Handling

- Menu registration fails: use JSON fallback config and log the failure.
- Config file missing: create default JSON config and continue.
- Config JSON invalid: use defaults and log the parse error.
- Config schema unsupported: use defaults and log the schema version.
- Enchantment keyword discovery fails: use fallback keyword list.
- Blacklist removes every keyword: skip enchantment and log once per reward batch.
- Single card enchantment fails: log and continue with the next card.
- Reward hook target not found: log and disable automatic enchantment patching.
- Missing or incompatible game API: fail closed for that operation and avoid breaking reward generation.

## Testing

Add focused tests under the existing xUnit project or a new test project if isolation is cleaner.

Pure logic tests:

- Menu defaults convert to runtime config with `enabled=true` and `enchant_chance=1.0`.
- Menu percent values clamp into `0..1`.
- JSON config normalize removes duplicate blacklist entries.
- Blacklisted keywords are excluded from the allowed candidate set.
- Empty allowed candidates skip enchantment.
- Cards that already have an enchantment are skipped.
- Probability `0%` never applies enchantments.
- Probability `100%` applies to every eligible card when candidates exist.
- `DeterministicSeed.FromString` returns the same value for the same input.

Build and smoke checks:

- `dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj`
- `powershell -ExecutionPolicy Bypass -File .\mods\CardRewardEnchantments\build.ps1`
- Confirm `dist` contains only `CardRewardEnchantments.dll` and `CardRewardEnchantments.json`.

Game smoke test:

- Start a run with the mod enabled and default chance.
- Open a card reward screen.
- Confirm eligible reward cards show one automatic enchantment.
- Add one keyword to the blacklist in the in-game menu.
- Confirm future automatic enchantments do not use that keyword.

## Open Implementation Questions

These must be resolved during implementation by inspecting `sts2.dll` and runtime behavior:

- Exact card reward generation method to patch.
- Exact API or fields for detecting existing card enchantments.
- Exact API or fields for applying an enchantment keyword to a card.
- Exact game data source for dynamic enchantment keyword discovery.

The design intentionally isolates those unknowns in `CardRewardEnchantInstaller`, `CardRewardAdapter`, and `EnchantmentKeywordCatalog`.

## Acceptance Criteria

- `CardRewardEnchantments` builds as an independent mod under `mods`.
- The mod registers an in-game config menu when BaseLib is available.
- The in-game menu exposes enable, probability, and checkbox-style blacklist controls.
- Default chance is `100%`.
- Automatic enchantment applies only to generated reward cards.
- Blacklisted keywords are not selected by automatic enchantment.
- Existing enchanted cards are not given another enchantment in the first release.
- Dynamic keyword discovery is attempted before fallback keywords are used.
- Precise enchantment selection is not visible or active in the first release.
- Shared config loading, menu registration, and deterministic seed helpers live under `utils`.
