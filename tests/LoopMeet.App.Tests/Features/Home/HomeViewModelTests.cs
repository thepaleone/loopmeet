using LoopMeet.App.Features.Home.ViewModels;

namespace LoopMeet.App.Tests.Features.Home;

public sealed class HomeViewModelTests
{
    [Fact]
    public void Constructor_SetsPlaceholderCopy()
    {
        var viewModel = new HomeViewModel();

        Assert.Equal("Home", viewModel.Title);
        Assert.Contains("coming soon", viewModel.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("placeholder", viewModel.SupportingText, StringComparison.OrdinalIgnoreCase);
    }
}
