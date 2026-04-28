using System;

namespace CardRewardEnchantments.Features.CardRewards;

public sealed record EnchantmentDefinition(string Keyword, Type? EnchantmentType = null);
