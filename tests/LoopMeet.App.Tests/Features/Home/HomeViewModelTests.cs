namespace LoopMeet.App.Tests.Features.Home;

public sealed class HomeViewModelTests
{
    [Fact]
    public void HomeViewModel_HasPlaceholderCopyAndGreeting()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("Hello", source, StringComparison.Ordinal);
        Assert.Contains("coming soon", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("placeholder", source, StringComparison.OrdinalIgnoreCase);
    }
}
