namespace LoopMeet.App.Tests.Features.Auth;

// Native-only (Sign in with Apple), so verified by source inspection; quickstart row 13 covers it on device.
public sealed class AppleAuthorizationSurfaceTests
{
    [Fact]
    public void AuthService_StartsRegistrationAfterAppleSignInWithoutAwaitingIt()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AuthService.cs");
        var appleSignIn = Slice(source, "public async Task<OAuthSignInResult> SignInWithAppleAsync()", "#else");

        var signIn = appleSignIn.IndexOf("SignInWithIdToken", StringComparison.Ordinal);
        var register = appleSignIn.IndexOf("_appleAuthorizationRegistrar.RegisterAsync(", StringComparison.Ordinal);
        Assert.True(signIn >= 0, "SignInWithAppleAsync no longer calls SignInWithIdToken");
        Assert.True(register > signIn, "registration must start after SignInWithIdToken succeeds");

        Assert.Contains("credential.AuthorizationCode", appleSignIn, StringComparison.Ordinal);
        Assert.Contains("session is not null", appleSignIn, StringComparison.Ordinal);
        Assert.Contains("_ = _appleAuthorizationRegistrar.RegisterAsync(authorizationCode);", appleSignIn, StringComparison.Ordinal);
        Assert.DoesNotContain("await _appleAuthorizationRegistrar", appleSignIn, StringComparison.Ordinal);
    }

    [Fact]
    public void Registrar_InvokesTheEdgeFunctionAndCatchesEveryException()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AppleAuthorizationRegistrar.cs");

        Assert.Contains("\"apple-authorization\"", source, StringComparison.Ordinal);
        Assert.Contains("_client.Functions.Invoke(FunctionName, accessToken", source, StringComparison.Ordinal);
        Assert.Contains("[\"authorizationCode\"] = authorizationCode", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception ex)", source, StringComparison.Ordinal);
        Assert.Contains("_logger.LogWarning(\"AppleAuthorizationFailed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw", Slice(source, "public async Task RegisterAsync", "\n    }\n}"), StringComparison.Ordinal);
    }

    [Fact]
    public void Registrar_NeverLogsTheAuthorizationCode()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/Features/Auth/AppleAuthorizationRegistrar.cs");

        foreach (var line in source.Split('\n').Where(line => line.Contains("_logger.Log", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain("authorizationCode", line, StringComparison.Ordinal);
            Assert.DoesNotContain("accessToken", line, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MauiProgram_RegistersTheRegistrar()
    {
        var source = ReadSource("../../../../../src/LoopMeet.App/MauiProgram.cs");

        Assert.Contains("AddSingleton<AppleAuthorizationRegistrar>()", source, StringComparison.Ordinal);
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
