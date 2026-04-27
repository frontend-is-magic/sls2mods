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
    private readonly Action<string> _log;

    public CardRewardAdapter(Action<string> log)
    {
        _log = log;
    }

    public bool HasEnchantment(object card)
    {
        var type = card.GetType();
        var singleProperty = type.GetProperty("Enchantment") ?? type.GetProperty("Enchant");
        if (singleProperty != null)
        {
            return singleProperty.GetValue(card) != null;
        }

        var collectionProperty = type.GetProperty("Enchantments") ?? type.GetProperty("EnchantmentsData");
        return collectionProperty?.GetValue(card) is ICollection collection && collection.Count > 0;
    }

    public bool TryApplyEnchantment(object card, string keyword, out string failureReason)
    {
        try
        {
            if (TryApplyWithCardMethod(card, keyword, out failureReason))
            {
                return true;
            }

            if (TryApplyWithCardCmd(card, keyword, out failureReason))
            {
                return true;
            }

            _log($"CardRewardAdapter: {failureReason}");
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
        var method = type.GetMethod("AddEnchantment") ?? type.GetMethod("ApplyEnchantment");
        if (method == null)
        {
            failureReason = $"no card-local enchantment method on {type.FullName}";
            return false;
        }

        method.Invoke(card, new object[] { keyword });
        failureReason = string.Empty;
        return true;
    }

    private static bool TryApplyWithCardCmd(object card, string keyword, out string failureReason)
    {
        var assembly = card.GetType().Assembly;
        var modelDbType = FindType("MegaCrit.Sts2.Core.Models.ModelDb", assembly);
        var modelIdType = FindType("MegaCrit.Sts2.Core.Models.ModelId", assembly);
        var enchantmentModelType = FindType("MegaCrit.Sts2.Core.Models.EnchantmentModel", assembly);
        var cardCmdType = FindType("MegaCrit.Sts2.Core.Commands.CardCmd", assembly);
        if (modelDbType == null || modelIdType == null || enchantmentModelType == null || cardCmdType == null)
        {
            failureReason = $"no supported enchantment API for {card.GetType().FullName}";
            return false;
        }

        var modelId = CreateModelId(modelIdType, enchantmentModelType, keyword);
        var getById = modelDbType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "GetById" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
        if (getById == null)
        {
            failureReason = "ModelDb.GetById<T>(ModelId) was not found";
            return false;
        }

        var immutableEnchantment = getById.MakeGenericMethod(enchantmentModelType).Invoke(null, new[] { modelId });
        var enchantment = immutableEnchantment?.GetType().GetMethod("ToMutable")?.Invoke(immutableEnchantment, null)
            ?? immutableEnchantment;
        if (enchantment == null)
        {
            failureReason = $"enchantment '{keyword}' was not found";
            return false;
        }

        var canEnchant = enchantment.GetType().GetMethod("CanEnchant", new[] { card.GetType() });
        if (canEnchant?.Invoke(enchantment, new[] { card }) is false)
        {
            failureReason = $"enchantment '{keyword}' cannot enchant this card";
            return false;
        }

        var enchantMethod = cardCmdType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "Enchant" && method.GetParameters().Length == 3);
        if (enchantMethod == null)
        {
            failureReason = "CardCmd.Enchant(enchantment, card, amount) was not found";
            return false;
        }

        enchantMethod.Invoke(null, new[] { enchantment, card, 1m });
        failureReason = string.Empty;
        return true;
    }

    private static object CreateModelId(Type modelIdType, Type enchantmentModelType, string keyword)
    {
        var slugifyCategory = modelIdType.GetMethod("SlugifyCategory", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ModelId.SlugifyCategory<T>() was not found");
        var category = slugifyCategory.MakeGenericMethod(enchantmentModelType).Invoke(null, null);
        return Activator.CreateInstance(modelIdType, category, keyword.ToUpperInvariant())
            ?? throw new InvalidOperationException("ModelId constructor returned null");
    }

    private static Type? FindType(string fullName, Assembly preferredAssembly)
    {
        return preferredAssembly.GetType(fullName, throwOnError: false)
            ?? Type.GetType(fullName)
            ?? AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, throwOnError: false))
                .FirstOrDefault(type => type != null);
    }
}
