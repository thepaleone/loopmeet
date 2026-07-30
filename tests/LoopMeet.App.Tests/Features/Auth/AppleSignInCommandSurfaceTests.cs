namespace LoopMeet.App.Tests.Features.Auth;

public sealed class AppleSignInCommandSurfaceTests
{
    [Fact]
    public void LoginViewModel_SourceContainsAppleSignInGuardAndCommand()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs");

        Assert.Contains("#if IOS || MACCATALYST", source, StringComparison.Ordinal);
        Assert.Contains("SignInWithAppleAsync", source, StringComparison.Ordinal);
        Assert.Contains("ShowAppleSignIn", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppleAuthCredentialProvider_SourceIsApplePlatformOnly()
    {
        var source = ReadSource(
            "../../../../../src/LoopMeet.App/Features/Auth/Platforms/Apple/AppleAuthCredentialProvider.cs");

        Assert.Contains("#if IOS || MACCATALYST", source, StringComparison.Ordinal);
        Assert.Contains("ASAuthorizationAppleIdProvider", source, StringComparison.Ordinal);
        Assert.Contains("RequestCredentialAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppleAuthNonce_SourceUsesStdlibCryptography()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AppleAuthNonce.cs");

        Assert.Contains("RandomNumberGenerator", source, StringComparison.Ordinal);
        Assert.Contains("SHA256", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthService_DeclaresSignInWithAppleAsync()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AuthService.cs");

        Assert.Contains("SignInWithAppleAsync", source, StringComparison.Ordinal);
        Assert.Contains("#if IOS || MACCATALYST", source, StringComparison.Ordinal);
        Assert.Contains("SignInWithIdToken", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginPage_XamlContainsAppleButtonBoundToVisibilityAndCommand()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/Views/LoginPage.xaml");

        Assert.Contains("Continue with Apple", source, StringComparison.Ordinal);
        Assert.Contains("{Binding ShowAppleSignIn}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding SignInWithAppleCommand}", source, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        return File.ReadAllText(fullPath);
    }
}
