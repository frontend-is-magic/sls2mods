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

    bool TryApplyEnchantment(object card, string keyword, out string failureReason);
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

    public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
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

                var modelId = new ModelId(
                    ModelId.SlugifyCategory<EnchantmentModel>(),
                    keyword.ToUpperInvariant());
                var enchantment = ModelDb.GetById<EnchantmentModel>(modelId).ToMutable();
                if (!enchantment.CanEnchant(cardModel))
                {
                    failureReason = $"{keyword} cannot enchant this card";
                    return false;
                }

                CardCmd.Enchant(enchantment, cardModel, 1m);
                failureReason = string.Empty;
                return true;
            }

            if (TryApplyWithCardMethod(card, keyword, out failureReason))
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

    public string BuildRewardKey(object instance, object? result)
    {
        var instancePart = SafeHash(instance);
        var resultPart = result == null ? "void" : SafeHash(result);
        return $"{instance.GetType().FullName}|instance={instancePart}|result={resultPart}";
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

    private static string SafeHash(object value)
    {
        try
        {
            return value.GetHashCode().ToString("X");
        }
        catch
        {
            return "unknown";
        }
    }
}
