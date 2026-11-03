namespace LoopMeet.App.Tests.Features.Auth;

// Regression for email sign-up crashing with a 401 from POST /users/profile. Gotrue only adopts a
// sign-up session when the user's confirmed_at is set, and Supabase Auth returns email_confirmed_at
// without it, so the profile request went out with no bearer token and the unhandled ApiException
// killed the app. Native/MAUI code, so verified by source inspection.
public sealed class EmailSignUpSurfaceTests
{
    [Fact]
    public void SignUpAdoptsTheReturnedSessionWhenGotrueDidNotStoreIt()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AuthService.cs");
        var signUp = Slice(source, "public async Task<AuthSession> SignUpWithEmailAsync", "public async Task<OAuthSignInResult> SignInWithGoogleAsync");

        var signUpCall = signUp.IndexOf("_client.Auth.SignUp(email, password)", StringComparison.Ordinal);
        var adopt = signUp.IndexOf("_client.Auth.SetSession(response.AccessToken, response.RefreshToken)", StringComparison.Ordinal);
        Assert.True(signUpCall >= 0, "SignUpWithEmailAsync no longer calls SignUp");
        Assert.True(adopt > signUpCall, "the returned session must be adopted after SignUp");
        Assert.Contains("_client.Auth.CurrentSession is null", signUp, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateAccountShowsAnErrorInsteadOfCrashing()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs");
        var save = Slice(source, "private async Task SaveAsync()", "public void ApplyPrefill");

        Assert.Contains("catch (Exception ex)", save, StringComparison.Ordinal);
        Assert.Contains("ErrorMessage = SignUpErrorMessage(ex);", save, StringComparison.Ordinal);
        Assert.Contains("ShowError = true;", save[save.IndexOf("catch (Exception ex)", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void RetryingAfterAProfileFailureDoesNotSignUpAgain()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs");
        var save = Slice(source, "private async Task SaveAsync()", "public void ApplyPrefill");

        Assert.Contains("_signedUpEmail", save, StringComparison.Ordinal);
        var guard = save.IndexOf("!string.Equals(_signedUpEmail, Email", StringComparison.Ordinal);
        var signUp = save.IndexOf("_authService.SignUpWithEmailAsync(Email, Password)", StringComparison.Ordinal);
        Assert.True(guard >= 0 && signUp > guard, "sign-up must be skipped once this email has signed up");
    }

    private static string Slice(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' not found");
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(to > from, $"'{end}' not found after '{start}'");
        return source[from..to];
    }

    private static string ReadSource(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativePath));
        return File.ReadAllText(fullPath);
    }
}
