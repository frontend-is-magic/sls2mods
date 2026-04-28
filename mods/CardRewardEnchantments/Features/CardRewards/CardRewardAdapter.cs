using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace CardRewardEnchantments.Features.CardRewards;

public interface ICardRewardEnchantAdapter
{
    bool HasEnchantment(object card);

    bool CanEnchant(object card, EnchantmentDefinition definition, out string failureReason);

    bool TryApplyEnchantment(object card, EnchantmentDefinition definition, out string failureReason);
}

public sealed class CardRewardAdapter : ICardRewardEnchantAdapter
{
    public bool HasEnchantment(object card)
    {
        try
        {
            if (card is CardModel cardModel)
            {
                return cardModel.Enchantment != null;
            }

            var type = card.GetType();
            var singleProperty = type.GetProperty("Enchantment") ?? type.GetProperty("Enchant");
            if (singleProperty != null && singleProperty.GetValue(card) != null)
            {
                return true;
            }

            var collectionProperty = type.GetProperty("Enchantments") ?? type.GetProperty("EnchantmentsData");
            return collectionProperty?.GetValue(card) is ICollection collection && collection.Count > 0;
        }
        catch
        {
            return true;
        }
    }

    public bool CanEnchant(object card, EnchantmentDefinition definition, out string failureReason)
    {
        try
        {
            if (card is not CardModel cardModel)
            {
                failureReason = string.Empty;
                return true;
            }

            var enchantment = CreateEnchantment(definition);
            if (!enchantment.CanEnchant(cardModel))
            {
                failureReason = $"{definition.Keyword} cannot enchant this card";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            failureReason = ex.InnerException?.Message ?? ex.Message;
            return false;
        }
    }

    public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
    {
        return TryApplyEnchantment(card, new EnchantmentDefinition(keyword), out failureReason);
    }

    public bool TryApplyEnchantment(object card, EnchantmentDefinition definition, out string failureReason)
    {
        try
        {
            if (card is CardModel cardModel)
            {
                if (cardModel.Enchantment != null)
                {
                    failureReason = "card already has an enchantment";
                    return false;
                }

                var enchantment = CreateEnchantment(definition);
                if (!enchantment.CanEnchant(cardModel))
                {
                    failureReason = $"{definition.Keyword} cannot enchant this card";
                    return false;
                }

                var amount = EnchantmentAmountCatalog.GetAmount(definition.Keyword);
                CardCmd.Enchant(enchantment, cardModel, amount);
                failureReason = string.Empty;
                return true;
            }

            if (TryApplyWithCardMethod(card, definition.Keyword, out failureReason))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            failureReason = ex.InnerException?.Message ?? ex.Message;
            return false;
        }
    }

    private static EnchantmentModel CreateEnchantment(EnchantmentDefinition definition)
    {
        if (definition.EnchantmentType != null)
        {
            if (TryGetRegisteredEnchantment(definition.EnchantmentType, out var model))
            {
                return model.ToMutable();
            }

            if (Activator.CreateInstance(definition.EnchantmentType) is EnchantmentModel created)
            {
                return created;
            }
        }

        var modelId = new ModelId(
            ModelId.SlugifyCategory<EnchantmentModel>(),
            definition.Keyword.ToUpperInvariant());
        return ModelDb.GetById<EnchantmentModel>(modelId).ToMutable();
    }

    private static bool TryGetRegisteredEnchantment(Type enchantmentType, out EnchantmentModel model)
    {
        try
        {
            var method = typeof(ModelDb).GetMethod(
                "Get",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(Type) },
                modifiers: null);
            if (method?.Invoke(null, new object[] { enchantmentType }) is EnchantmentModel registered)
            {
                model = registered;
                return true;
            }
        }
        catch
        {
            // Some concrete enchantment classes are not registered in ModelDb.
        }

        model = null!;
        return false;
    }

    public IEnumerable<object> ExtractRewardCards(object instance, object? result)
    {
        if (result is Task task && !task.IsCompletedSuccessfully)
        {
            yield break;
        }

        if (instance is not CardReward reward)
        {
            yield break;
        }

        foreach (var card in reward.Cards)
        {
            if (card != null)
            {
                yield return card;
            }
        }
    }

    public string BuildRewardKey(IEnumerable<object> cards)
    {
        var cardParts = cards.Select(BuildCardKey);
        return $"CardReward|cards={string.Join(",", cardParts)}";
    }

    private static bool TryApplyWithCardMethod(object card, string keyword, out string failureReason)
    {
        var type = card.GetType();
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(method =>
                (method.Name == "AddEnchantment" || method.Name == "ApplyEnchantment")
                && method.GetParameters() is [var parameter]
                && parameter.ParameterType == typeof(string));
        if (method == null)
        {
            failureReason = $"no supported enchantment API for {type.FullName}";
            return false;
        }

        method.Invoke(card, new object[] { keyword });
        failureReason = string.Empty;
        return true;
    }

    private static string BuildCardKey(object card)
    {
        var type = card.GetType();
        var id = TryReadStableValue(card, "Id")
            ?? TryReadStableValue(card, "ID")
            ?? TryReadStableValue(card, "ModelId")
            ?? TryReadStableValue(card, "Name")
            ?? TryReadStableValue(card, "Title")
            ?? type.FullName
            ?? type.Name;
        return $"{type.FullName}:{id}";
    }

    private static string? TryReadStableValue(object value, string memberName)
    {
        try
        {
            var type = value.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            var propertyValue = property?.GetValue(value);
            if (propertyValue != null)
            {
                return propertyValue.ToString();
            }

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(value)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
