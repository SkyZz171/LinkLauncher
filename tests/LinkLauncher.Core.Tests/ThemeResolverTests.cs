namespace LinkLauncher.Core.Tests;

using LinkLauncher.Core.Models;
using LinkLauncher.Core.Services;
using Xunit;

public sealed class ThemeResolverTests
{
    [Theory]
    [InlineData(AppTheme.Light, AppTheme.Dark, AppTheme.Light)]
    [InlineData(AppTheme.Dark, AppTheme.Light, AppTheme.Dark)]
    [InlineData(AppTheme.System, AppTheme.Light, AppTheme.Light)]
    [InlineData(AppTheme.System, AppTheme.Dark, AppTheme.Dark)]
    [InlineData(AppTheme.System, AppTheme.System, AppTheme.Dark)]
    public void ResolveEffective_ReturnsExpectedTheme(AppTheme requested, AppTheme systemTheme, AppTheme expected)
    {
        Assert.Equal(expected, ThemeResolver.ResolveEffective(requested, systemTheme));
    }
}
