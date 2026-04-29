using Xunit;

namespace MapNodeChanger.Tests;

public sealed class AllSnakebiteSourceTests
{
    [Fact]
    public void ModUsesSnakebiteGameplayPatchPoints()
    {
        var source = File.ReadAllText(FindRepoFile("mods/AllSnakebite/AllSnakebite.cs"));

        Assert.Contains("private const string ModId = \"AllSnakebite\"", source);
        Assert.Contains("CardKeyword.Retain", source);
        Assert.Contains("nameof(CardModel.Title)", source);
        Assert.Contains("CreatureCmd", source);
        Assert.Contains("PowerCmd.Apply<PoisonPower>", source);
        Assert.Contains("CardType.Attack", source);
        Assert.Contains("return false", source);
    }

    [Fact]
    public void ModUsesSnakebiteDescriptionPatchPoints()
    {
        var source = File.ReadAllText(FindRepoFile("mods/AllSnakebite/AllSnakebite.cs"));

        Assert.Contains("nameof(CardModel.GetDescriptionForPile)", source);
        Assert.Contains("nameof(CardModel.GetDescriptionForUpgradePreview)", source);
        Assert.Contains("CardTextRewrite.RewriteDamageAsPoison", source);
        Assert.Contains("CardType.Attack", source);
    }

    [Fact]
    public void ModUsesConfigLoaderAndMenuRegistrar()
    {
        var source = File.ReadAllText(FindRepoFile("mods/AllSnakebite/AllSnakebite.cs"));

        Assert.Contains("ModConfigLoader.LoadOrCreate", source);
        Assert.Contains("ModConfigMenuRegistrar.TryRegister", source);
        Assert.Contains("AllSnakebiteConfigMenu.InitializeFrom", source);
        Assert.Contains("AllSnakebiteConfigMenu.ToRuntimeConfig", source);
    }

    [Fact]
    public void ModGatesRuntimePatchesBehindEnabledConfig()
    {
        var source = File.ReadAllText(FindRepoFile("mods/AllSnakebite/AllSnakebite.cs"));

        Assert.Contains("GetRuntimeConfig().Enabled", source);
        Assert.Contains("Sls2Mods.Utils.CardText", source);
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
