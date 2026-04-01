namespace LoopMeet.App.Tests.Features.Home;

public sealed class HomeViewModelTests
{
    [Fact]
    public void HomeViewModel_HasGreetingCopy()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("Hello", source, StringComparison.Ordinal);
    }
}
