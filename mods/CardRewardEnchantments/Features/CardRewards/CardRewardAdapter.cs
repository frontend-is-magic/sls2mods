using System;
using System.Collections;
using System.Reflection;

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
}
