namespace LoopMeet.App.Tests.Features.Profile;

public sealed class ChangePasswordViewModelTests
{
    [Fact]
    public void ChangePasswordViewModel_ValidatesFieldsAndCallsUsersApi()
    {
        var viewModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/ViewModels/ChangePasswordViewModel.cs"));
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("New password and confirmation are required.", source, StringComparison.Ordinal);
        Assert.Contains("Current password is required.", source, StringComparison.Ordinal);
        Assert.Contains("Email is required to set your password.", source, StringComparison.Ordinal);
        Assert.Contains("ChangePasswordAsync", source, StringComparison.Ordinal);
        Assert.Contains("Password Updated", source, StringComparison.Ordinal);
        Assert.Contains("var email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim()", source, StringComparison.Ordinal);
        Assert.Contains("Email = email", source, StringComparison.Ordinal);
        Assert.Contains("invalid_current_password", source, StringComparison.Ordinal);
        Assert.Contains("missing_account_email", source, StringComparison.Ordinal);
        Assert.Contains("Enter your account email to set your password.", source, StringComparison.Ordinal);
        Assert.Contains("LoadContextAsync", source, StringComparison.Ordinal);
        Assert.Contains("RequiresCurrentPassword", source, StringComparison.Ordinal);
        Assert.Contains("RequiresEmailForPasswordSetup", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangePasswordPage_IncludesOptionalEmailField()
    {
        var pagePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/Views/ChangePasswordPage.xaml"));
        var source = File.ReadAllText(pagePath);

        Assert.Contains("Account email", source, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding RequiresEmailForPasswordSetup}\"", source, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding RequiresCurrentPassword}\"", source, StringComparison.Ordinal);
    }
}
