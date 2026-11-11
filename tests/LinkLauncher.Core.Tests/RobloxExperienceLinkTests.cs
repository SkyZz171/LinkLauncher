namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxExperienceLinkTests
{
    [Theory]
    [InlineData("920587237", 920587237L)]
    [InlineData(" 920587237 ", 920587237L)]
    [InlineData("https://www.roblox.com/games/920587237/Adopt-Me", 920587237L)]
    [InlineData("https://roblox.com/games/920587237", 920587237L)]
    [InlineData("http://web.roblox.com/games/920587237/Adopt-Me?foo=1", 920587237L)]
    [InlineData("https://WWW.ROBLOX.COM/games/920587237", 920587237L)]
    public void TryCreate_WithOfficialLink_ReturnsCanonicalPage(string input, long placeId)
    {
        Assert.True(RobloxExperienceLink.TryCreate(input, out var reference));
        Assert.NotNull(reference);
        Assert.Equal(placeId, reference!.PlaceId);
        Assert.Equal($"https://www.roblox.com/games/{placeId}", reference.Page.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Adopt Me")]
    [InlineData("https://example.com/games/920587237")]
    [InlineData("https://www.roblox.com/catalog/920587237")]
    [InlineData("https://www.roblox.com/games/abc")]
    [InlineData("https://roblox.com/games")]
    [InlineData("0")]
    [InlineData("-12")]
    [InlineData("roblox-player:1+launchmode:app")]
    [InlineData("javascript:alert(1)")]
    public void TryCreate_WithUnsupportedInput_ReturnsFalse(string? input)
    {
        Assert.False(RobloxExperienceLink.TryCreate(input, out var reference));
        Assert.Null(reference);
    }

    [Fact]
    public void ExperiencePage_WithInvalidPlaceId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RobloxWebUrls.ExperiencePage(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => RobloxWebUrls.ExperiencePage(-5));
    }
}
