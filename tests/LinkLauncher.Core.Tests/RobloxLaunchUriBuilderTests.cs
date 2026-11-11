namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class RobloxLaunchUriBuilderTests
{
    [Fact]
    public void ClientLaunchUri_StartsClientWithoutTicket()
    {
        Assert.Equal("roblox-player:1+launchmode:app", RobloxLaunchUriBuilder.ClientLaunchUri);
        Assert.DoesNotContain("gameinfo", RobloxLaunchUriBuilder.ClientLaunchUri, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("launchmode:play", RobloxLaunchUriBuilder.ClientLaunchUri, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildClientLaunchUri_WithoutLocale_ReturnsBaseUri(string? locale)
    {
        Assert.Equal(RobloxLaunchUriBuilder.ClientLaunchUri, RobloxLaunchUriBuilder.BuildClientLaunchUri(locale));
    }

    [Theory]
    [InlineData("fr_FR", "fr_FR")]
    [InlineData("en-US", "en_US")]
    [InlineData("pt_BR", "pt_BR")]
    public void BuildClientLaunchUri_WithLocale_AppendsBothLocales(string locale, string expected)
    {
        var uri = RobloxLaunchUriBuilder.BuildClientLaunchUri(locale);

        Assert.StartsWith(RobloxLaunchUriBuilder.ClientLaunchUri, uri, StringComparison.Ordinal);
        Assert.Contains($"+robloxLocale:{expected}", uri, StringComparison.Ordinal);
        Assert.Contains($"+gameLocale:{expected}", uri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("en_us+launchmode:play")]
    [InlineData("en_us:evil")]
    [InlineData("../../secret")]
    [InlineData("locale with spaces")]
    [InlineData("locale&param=1")]
    [InlineData("unelocalebeaucouptroplongue")]
    public void BuildClientLaunchUri_WithUnsafeLocale_FallsBackToBaseUri(string locale)
    {
        Assert.False(RobloxLaunchUriBuilder.IsSafeLocale(locale));
        Assert.Equal(RobloxLaunchUriBuilder.ClientLaunchUri, RobloxLaunchUriBuilder.BuildClientLaunchUri(locale));
    }
}
