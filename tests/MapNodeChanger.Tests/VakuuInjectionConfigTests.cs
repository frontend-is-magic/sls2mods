using VakuuRoomInjection.Features.Vakuu;
using Xunit;

namespace VakuuRoomInjection.Tests;

public sealed class VakuuInjectionConfigTests
{
    [Fact]
    public void MenuDefaultsConvertToRuntimeConfig()
    {
        VakuuRoomInjectionConfigMenu.Enabled = true;
        VakuuRoomInjectionConfigMenu.AncientTarget = AncientTarget.Vakuu;
        VakuuRoomInjectionConfigMenu.OtherRoomChancePercent = 6.6;
        VakuuRoomInjectionConfigMenu.UnknownRoomChancePercent = 66;

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig();

        Assert.True(config.Enabled);
        Assert.Equal(AncientTarget.Vakuu, config.AncientTarget);
        Assert.Equal(0.066, config.OtherRoomChance, 3);
        Assert.Equal(0.66, config.UnknownRoomChance, 3);
    }

    [Fact]
    public void MenuCanInitializeAndPersistDisabledConfig()
    {
        VakuuRoomInjectionConfigMenu.InitializeFrom(new VakuuInjectionConfig
        {
            Enabled = false,
            AncientTarget = AncientTarget.Pael,
            OtherRoomChance = 0.25,
            UnknownRoomChance = 0.75
        });

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig(new VakuuInjectionConfig { Enabled = true });

        Assert.False(VakuuRoomInjectionConfigMenu.Enabled);
        Assert.False(config.Enabled);
        Assert.Equal(AncientTarget.Pael, config.AncientTarget);
        Assert.Equal(0.25, config.OtherRoomChance);
        Assert.Equal(0.75, config.UnknownRoomChance);
    }

    [Fact]
    public void MenuSourceAddsEnabledOptionBeforeGeneratedOptions()
    {
        var source = File.ReadAllText(FindRepoFile("mods/VakuuRoomInjection/Features/Vakuu/VakuuRoomInjectionConfigMenu.cs"));

        Assert.True(
            source.IndexOf("AddEnabledOption", StringComparison.Ordinal) <
            source.IndexOf("GenerateOptionsForAllProperties", StringComparison.Ordinal));
        Assert.Contains("ConfigHideInUI", source);
    }

    [Fact]
    public void MenuPercentValuesClampToValidProbabilities()
    {
        VakuuRoomInjectionConfigMenu.Enabled = true;
        VakuuRoomInjectionConfigMenu.AncientTarget = AncientTarget.Pael;
        VakuuRoomInjectionConfigMenu.OtherRoomChancePercent = -50;
        VakuuRoomInjectionConfigMenu.UnknownRoomChancePercent = 150;

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig();

        Assert.Equal(AncientTarget.Pael, config.AncientTarget);
        Assert.Equal(0, config.OtherRoomChance);
        Assert.Equal(1, config.UnknownRoomChance);
    }

    [Fact]
    public void MenuCanSelectRandomAncientTarget()
    {
        VakuuRoomInjectionConfigMenu.Enabled = true;
        VakuuRoomInjectionConfigMenu.AncientTarget = AncientTarget.Random;
        VakuuRoomInjectionConfigMenu.OtherRoomChancePercent = 6.6;
        VakuuRoomInjectionConfigMenu.UnknownRoomChancePercent = 66;

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig();

        Assert.Equal(AncientTarget.Random, config.AncientTarget);
    }

    [Fact]
    public void RandomAncientTargetResolvesToConcreteTarget()
    {
        var rng = new Random(123);

        var target = VakuuInjectionRule.ResolveAncientTarget(AncientTarget.Random, rng);

        Assert.NotEqual(AncientTarget.Random, target);
        Assert.Contains(target, VakuuInjectionRule.ConcreteAncientTargets);
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find {relativePath}");
    }
}
