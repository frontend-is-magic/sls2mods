using VakuuRoomInjection.Features.Vakuu;
using Xunit;

namespace VakuuRoomInjection.Tests;

public sealed class VakuuInjectionConfigTests
{
    [Fact]
    public void MenuDefaultsConvertToRuntimeConfig()
    {
        VakuuRoomInjectionConfigMenu.AncientTarget = AncientTarget.Vakuu;
        VakuuRoomInjectionConfigMenu.OtherRoomChancePercent = 6.6;
        VakuuRoomInjectionConfigMenu.UnknownRoomChancePercent = 66;

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig();

        Assert.Equal(AncientTarget.Vakuu, config.AncientTarget);
        Assert.Equal(0.066, config.OtherRoomChance, 3);
        Assert.Equal(0.66, config.UnknownRoomChance, 3);
    }

    [Fact]
    public void MenuPercentValuesClampToValidProbabilities()
    {
        VakuuRoomInjectionConfigMenu.AncientTarget = AncientTarget.Pael;
        VakuuRoomInjectionConfigMenu.OtherRoomChancePercent = -50;
        VakuuRoomInjectionConfigMenu.UnknownRoomChancePercent = 150;

        var config = VakuuRoomInjectionConfigMenu.ToRuntimeConfig();

        Assert.Equal(AncientTarget.Pael, config.AncientTarget);
        Assert.Equal(0, config.OtherRoomChance);
        Assert.Equal(1, config.UnknownRoomChance);
    }
}
