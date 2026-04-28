using AllSnakebite;
using Xunit;

namespace MapNodeChanger.Tests;

public sealed class AllSnakebiteConfigTests
{
    public AllSnakebiteConfigTests()
    {
        AllSnakebiteConfigMenu.InitializeFrom(new AllSnakebiteConfig());
    }

    [Fact]
    public void RuntimeConfigDefaultsToEnabled()
    {
        var config = new AllSnakebiteConfig();

        Assert.Equal(1, config.SchemaVersion);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void RuntimeConfigNormalizeRestoresDefaultSchema()
    {
        var config = new AllSnakebiteConfig
        {
            SchemaVersion = 0,
            Enabled = false
        };

        config.Normalize();

        Assert.Equal(1, config.SchemaVersion);
        Assert.False(config.Enabled);
    }

    [Fact]
    public void MenuCanInitializeFromRuntimeConfig()
    {
        AllSnakebiteConfigMenu.InitializeFrom(new AllSnakebiteConfig { Enabled = false });

        var config = AllSnakebiteConfigMenu.ToRuntimeConfig();

        Assert.False(AllSnakebiteConfigMenu.Enabled);
        Assert.False(config.Enabled);
    }

    [Fact]
    public void MenuCanUpdateFallbackConfig()
    {
        AllSnakebiteConfigMenu.Enabled = false;
        var fallback = new AllSnakebiteConfig { Enabled = true };

        var config = AllSnakebiteConfigMenu.ToRuntimeConfig(fallback);

        Assert.Same(fallback, config);
        Assert.False(config.Enabled);
    }
}
