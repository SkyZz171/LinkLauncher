namespace LinkLauncher.Core.Tests;

using LinkLauncher.Features.Navigation;
using Xunit;

public sealed class NavigationServiceTests
{
    [Fact]
    public void NavigateTo_FirstRoute_SetsPageAndRaisesEvent()
    {
        var factory = new CountingPageFactory();
        var navigation = new NavigationService(factory);
        NavigationChangedEventArgs? raised = null;
        navigation.Navigated += (_, args) => raised = args;

        navigation.NavigateTo(NavigationRoutes.Dashboard);

        Assert.Equal(NavigationRoutes.Dashboard, navigation.CurrentRoute);
        Assert.NotNull(raised);
        Assert.Equal(NavigationRoutes.Dashboard, raised!.Route);
        Assert.Same(navigation.CurrentPage, raised.Page);
        Assert.Equal(1, factory.CallCount);
    }

    [Fact]
    public void NavigateTo_WhenPageAlreadyVisited_KeepsInstance()
    {
        var factory = new CountingPageFactory();
        var navigation = new NavigationService(factory);

        navigation.NavigateTo(NavigationRoutes.Dashboard);
        var firstPage = navigation.CurrentPage;
        navigation.NavigateTo(NavigationRoutes.Settings);
        navigation.NavigateTo(NavigationRoutes.Dashboard);

        Assert.Same(firstPage, navigation.CurrentPage);
        Assert.Equal(2, factory.CallCount);
    }

    [Fact]
    public void NavigateTo_SameRoute_DoesNotRaiseEventAgain()
    {
        var navigation = new NavigationService(new CountingPageFactory());
        var raisedCount = 0;
        navigation.Navigated += (_, _) => raisedCount++;

        navigation.NavigateTo(NavigationRoutes.Dashboard);
        navigation.NavigateTo(NavigationRoutes.Dashboard);

        Assert.Equal(1, raisedCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NavigateTo_InvalidRoute_Throws(string route)
    {
        var navigation = new NavigationService(new CountingPageFactory());

        Assert.ThrowsAny<ArgumentException>(() => navigation.NavigateTo(route));
    }

    private sealed class CountingPageFactory : INavigationPageFactory
    {
        public int CallCount { get; private set; }

        public object CreatePage(string route)
        {
            CallCount++;
            return new object();
        }
    }
}
