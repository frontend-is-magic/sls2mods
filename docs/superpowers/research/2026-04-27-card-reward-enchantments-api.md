# Card Reward Enchantments API Notes

## Reward Hook Target

- Target type: `MegaCrit.Sts2.Core.Rewards.CardReward`
- Target method: `Populate() : Task`
- Patch type: postfix
- Reward card collection expression: `((MegaCrit.Sts2.Core.Rewards.CardReward)__instance).Cards`
- Why this target runs before the reward screen is shown: `MegaCrit.Sts2.Core.Rewards.RewardsSet.GenerateWithoutOffering()` awaits each `Reward.Populate()`, then runs `Hook.ModifyRewards(...)`, populates any newly added rewards, awaits `Hook.AfterModifyingRewards(...)`, sorts rewards, and returns. `RewardsSet.Offer()` only calls `Hook.BeforeRewardsOffered(...)` and `NRewardsScreen.ShowScreen(...).SetRewards(Rewards)` after `GenerateWithoutOffering()` completes. `CardReward.OnSelect()` opens `NCardRewardSelectionScreen.ShowScreen(...)` later when the reward button is selected, so a postfix on `CardReward.Populate()` mutates the reward card models before either the reward list screen or card choice screen is shown.

## Card Enchantment Operations

- Card type: `MegaCrit.Sts2.Core.Models.CardModel`
- Existing enchantment check: `card.Enchantment != null`
- Apply enchantment operation: resolve a mutable enchantment with `ModelDb.GetById<EnchantmentModel>(new ModelId(ModelId.SlugifyCategory<EnchantmentModel>(), keyword.ToUpperInvariant())).ToMutable()`, optionally confirm `enchantment.CanEnchant(card)`, then call `MegaCrit.Sts2.Core.Commands.CardCmd.Enchant(enchantment, card, 1m)`.
- Keyword/id type used by the game: `MegaCrit.Sts2.Core.Models.ModelId` for `MegaCrit.Sts2.Core.Models.EnchantmentModel`; category is `ModelId.SlugifyCategory<EnchantmentModel>()` (`ENCHANTMENT`) and entry is the uppercase enchantment id such as `ADROIT`, `SWIFT`, or `VIGOROUS`.

## Keyword Discovery

- Dynamic source type: `MegaCrit.Sts2.Core.Models.ModelDb`
- Dynamic source member: `DebugEnchantments`, returning `IEnumerable<MegaCrit.Sts2.Core.Models.EnchantmentModel>`
- Normalized keyword string format: `enchantment.Id.Entry.Trim().ToLowerInvariant()`, e.g. `adroit`, `swift`, `vigorous`; convert back with `keyword.ToUpperInvariant()` when constructing the `ModelId`.

## Build Notes

- Additional DLLs needed for reflection: `.\sts2.dll` is absent in this worktree, so reflection was run against `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll` after loading sibling DLLs from the same directory, especially `GodotSharp.dll`, `0Harmony.dll`, and the `System.*.dll` runtime assemblies. `BaseLib.dll` was also inspected as supporting evidence, and `ilspycmd` was needed because `Assembly.GetTypes()` only surfaced 46 loadable public/interface types while the decompiler showed the concrete reward, card, and enchantment classes.
- Any API risks: `CardReward.Populate()` returns `Task` but its card generation body is synchronous and returns `Task.CompletedTask`; a normal postfix sees the populated `_cards` immediately for the current build. If a future STS2 build makes `Populate()` genuinely asynchronous, `CardRewardAdapter` should patch an async continuation or use a postfix that awaits `__result` before reading `CardReward.Cards`. `CardCmd.Enchant(...)` throws when `EnchantmentModel.CanEnchant(card)` is false or when a non-stackable card already has another enchantment, so the adapter should check `card.Enchantment == null` and either call `CanEnchant` before applying or catch and log the exception.
