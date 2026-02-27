namespace LoopMeet.App.Tests.Features.Auth;

public sealed class LoginViewModelTests
{
    [Fact]
    public void LoginViewModel_IncludesSocialAvatarInProfileBootstrapRequest()
    {
        var viewModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs"));
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("SocialAvatarUrl = authResult.AvatarUrl", source, StringComparison.Ordinal);
    }
}
