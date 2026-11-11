namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Services;
using Xunit;

public sealed class DiscordApplicationIdRulesTests
{
    [Theory]
    [InlineData("123456789012345678", "123456789012345678")]
    [InlineData("  123456789012345678  ", "123456789012345678")]
    [InlineData("12345678901234567", "12345678901234567")]
    [InlineData("12345678901234567890", "12345678901234567890")]
    public void Normalize_WithValidIdentifier_ReturnsTrimmedValue(string input, string expected)
    {
        Assert.Equal(expected, DiscordApplicationIdRules.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456")]
    [InlineData("123456789012345678901")]
    [InlineData("12345678901234567a")]
    [InlineData("mon-identifiant")]
    public void Normalize_WithInvalidIdentifier_ReturnsNull(string? input)
    {
        Assert.Null(DiscordApplicationIdRules.Normalize(input));
    }
}
