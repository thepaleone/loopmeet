using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features.Profile;

/// <summary>
/// Source inspection of the redesigned Profile page (feature 022). Layout, bindings and
/// platform wiring can't run off a device, so these tests read the XAML, its code-behind
/// and the ViewModel.
/// </summary>
public sealed class ProfilePageLayoutTests
{
    private const string Page = "src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml";
    private const string ViewModel = "src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs";

    [Fact]
    public void ProfilePage_UsesOnlyResourceKeysThatExist()
    {
        var page = ReadSource(Page);
        var defined = new[] { "src/LoopMeet.App/Resources/Styles/Colors.xaml", "src/LoopMeet.App/Resources/Styles/Styles.xaml", Page }
            .SelectMany(path => Regex.Matches(ReadSource(path), "x:Key=\"([^\"]+)\"").Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        // A missing key doesn't fail the build: the property silently falls back to its default.
        var used = Regex.Matches(page, @"\{StaticResource\s+([^}\s]+)\}").Select(match => match.Groups[1].Value).Distinct();
        Assert.All(used, key => Assert.True(defined.Contains(key), $"ProfilePage uses StaticResource {key}, which isn't defined."));
    }

    [Fact]
    public void ProfilePage_HasNoHorizontalStackLayout()
    {
        // Regression: the old two button rows never wrapped, so Log out was clipped on narrow screens.
        Assert.DoesNotContain("HorizontalStackLayout", ReadSource(Page), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SaveProfileCommand")]
    [InlineData("OpenPasswordChangeCommand")]
    [InlineData("OpenNotificationSettingsCommand")]
    [InlineData("LogoutCommand")]
    [InlineData("DeleteAccountCommand")]
    [InlineData("PickAvatarCommand")]
    public void ProfilePage_BindsEveryActionToItsExistingCommand(string command)
    {
        Assert.Contains($"Command=\"{{Binding {command}}}\"", ReadSource(Page), StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_KeepsSaveOutsideTheScrollView()
    {
        var page = ReadSource(Page);

        // The 011 pattern: a title row above the ScrollView, so the keyboard can't cover Save.
        Assert.Contains("<Grid RowDefinitions=\"Auto,*\">", page, StringComparison.Ordinal);
        var save = page.IndexOf("SaveProfileCommand", StringComparison.Ordinal);
        var scroll = page.IndexOf("<ScrollView", StringComparison.Ordinal);
        Assert.True(save >= 0 && scroll > save, "Save must come before the ScrollView.");
    }

    [Fact]
    public void ProfilePage_EditsTheDisplayNameInPlace()
    {
        var page = ReadSource(Page);
        var account = page.IndexOf("Text=\"Account\"", StringComparison.Ordinal);
        var entry = page.IndexOf("<Entry", StringComparison.Ordinal);

        Assert.True(account >= 0 && entry > account, "The display name entry belongs in the Account card.");
        Assert.Contains("Text=\"{Binding DisplayName}\"", page[entry..page.IndexOf("/>", entry, StringComparison.Ordinal)], StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_ShowsChangePasswordOnlyWhenAllowed()
    {
        var page = ReadSource(Page);
        var command = page.IndexOf("Command=\"{Binding OpenPasswordChangeCommand}\"", StringComparison.Ordinal);
        var row = page.LastIndexOf("<Grid ", command, StringComparison.Ordinal);

        Assert.Contains("IsVisible=\"{Binding CanChangePassword}\"", page[row..command], StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_SwitchesColumnsByWidth()
    {
        var page = ReadSource(Page);
        var codeBehind = ReadSource(Page + ".cs");

        Assert.Contains("x:Name=\"HeaderRow\"", page, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ContentGrid\"", page, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"HeaderCard\"", page, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DetailsColumn\"", page, StringComparison.Ordinal);
        Assert.Contains("protected override void OnSizeAllocated", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ProfileLayout.UsesTwoColumns(", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ProfileLayout.MaxContentWidth(", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_RowsGrowWithText()
    {
        var page = ReadSource(Page);

        // Rows size to their content, so larger system text makes them taller, never clipped.
        Assert.DoesNotMatch(@"Style=""\{StaticResource SettingsRow\}""[^>]*HeightRequest=", page);
        foreach (var style in new[] { "RowTitle", "RowSubtitle" })
        {
            var start = page.IndexOf($"x:Key=\"{style}\"", StringComparison.Ordinal);
            Assert.True(start >= 0, $"Style {style} is missing.");
            var end = page.IndexOf("</Style>", start, StringComparison.Ordinal);
            Assert.Contains("<Setter Property=\"LineBreakMode\" Value=\"WordWrap\" />", page[start..end], StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProfilePage_SaveHasAccessibleDescriptionAndDimsWhenInactive()
    {
        var save = Element(ReadSource(Page), "Text=\"💾\"", "</Label>");

        Assert.Contains("SemanticProperties.Description=\"Save profile\"", save, StringComparison.Ordinal);
        Assert.Contains("<DataTrigger TargetType=\"Label\" Binding=\"{Binding CanSaveProfile}\" Value=\"False\">", save, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Opacity\" Value=\"0.4\" />", save, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_DisplayNameEntryIsNotGatedBySave()
    {
        // A ReturnCommand whose CanExecute is false can disable the Entry in MAUI, which would
        // stop typing while Save is inactive. Save is the header control only (analysis U1).
        var entry = Element(ReadSource(Page), "<Entry", "/>");

        Assert.DoesNotContain("ReturnCommand", entry, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_HeaderCardShowsTheSavedName()
    {
        var card = Element(ReadSource(Page), "x:Name=\"HeaderCard\"", "</Border>\n\n");

        Assert.Contains("Text=\"{Binding SavedDisplayName}\"", card, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding DisplayName}\"", card, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileViewModel_SaveRequiresAnUnsavedNonBlankName()
    {
        var viewModel = ReadSource(ViewModel);

        Assert.Contains("[RelayCommand(CanExecute = nameof(CanSaveProfile))]", viewModel, StringComparison.Ordinal);
        Assert.Contains("ProfileText.CanSave(SavedDisplayName, DisplayName)", viewModel, StringComparison.Ordinal);
        Assert.Contains("SavedDisplayName = profile.DisplayName;", viewModel, StringComparison.Ordinal);
        Assert.True(
            viewModel.Split("[NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]").Length - 1 >= 3,
            "IsBusy, DisplayName and SavedDisplayName must all re-evaluate Save.");
    }

    [Fact]
    public void ProfilePage_AvatarUsesTheBrandGradientAndCameraBadge()
    {
        var page = ReadSource(Page);

        // Style guide: the default avatar is an Amber Pulse to Guava Pink gradient with the initial.
        Assert.Contains("<LinearGradientBrush", page, StringComparison.Ordinal);
        Assert.Contains("<GradientStop Color=\"{StaticResource Primary}\" Offset=\"0\" />", page, StringComparison.Ordinal);
        Assert.Contains("<GradientStop Color=\"{StaticResource Secondary}\" Offset=\"1\" />", page, StringComparison.Ordinal);
        Assert.Contains("Source=\"ic_camera.png\"", page, StringComparison.Ordinal);
        Assert.True(File.Exists(RepoPath("src/LoopMeet.App/Resources/Images/ic_camera.svg")), "ic_camera.svg is missing.");
    }

    [Fact]
    public void ProfilePage_ShowsUploadProgressOnTheAvatar()
    {
        var page = ReadSource(Page);
        var avatar = Element(page, "<TapGestureRecognizer Command=\"{Binding PickAvatarCommand}\" />", "<ActivityIndicator");
        var indicator = Element(page, "<ActivityIndicator", "/>");

        Assert.EndsWith("<ActivityIndicator", avatar, StringComparison.Ordinal);
        Assert.Contains("IsRunning=\"{Binding IsUploading}\"", indicator, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SavedDisplayName")]
    [InlineData("Email")]
    public void ProfilePage_NameAndEmailTruncateOnOneLine(string property)
    {
        var label = Element(ReadSource(Page), $"Text=\"{{Binding {property}}}\"", "/>");

        Assert.Contains("MaxLines=\"1\"", label, StringComparison.Ordinal);
        Assert.Contains("LineBreakMode=\"TailTruncation\"", label, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_SummaryChipsWrap()
    {
        var chips = Element(ReadSource(Page), "<FlexLayout", "</FlexLayout>");

        Assert.Contains("Wrap=\"Wrap\"", chips, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding MemberSinceText}\"", chips, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding GroupCountText}\"", chips, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_HasNoDebugStyleLabels()
    {
        var page = ReadSource(Page);

        Assert.DoesNotContain("User since", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Groups:", page, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileViewModel_DerivesSummaryTexts()
    {
        var viewModel = ReadSource(ViewModel);

        Assert.Contains("MemberSinceText = ProfileText.MemberSince(profile.UserSince);", viewModel, StringComparison.Ordinal);
        Assert.Contains("GroupCountText = ProfileText.GroupCount(profile.GroupCount);", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_NotificationsRowShowsTheCurrentState()
    {
        var row = Element(ReadSource(Page), "Text=\"Notifications\"", "</Grid>");

        Assert.Contains("Text=\"{Binding NotificationStatusText}\"", row, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_RefreshesNotificationStateOnEveryAppearance()
    {
        var viewModel = ReadSource(ViewModel);
        var onAppearing = Element(ReadSource(Page + ".cs"), "protected override void OnAppearing()", "\n    }\n");

        Assert.Contains("public void RefreshNotificationStatus()", viewModel, StringComparison.Ordinal);
        Assert.Contains("ProfileText.NotificationStatus(_notificationPermissionService.CurrentState)", viewModel, StringComparison.Ordinal);
        // LoadCommand ignores calls while a load runs, so the refresh must not go through it (analysis U2).
        var refresh = onAppearing.IndexOf("RefreshNotificationStatus()", StringComparison.Ordinal);
        var load = onAppearing.IndexOf("LoadCommand.Execute", StringComparison.Ordinal);
        Assert.True(refresh >= 0 && load > refresh, "OnAppearing must refresh the notification state directly, before the load.");
    }

    [Fact]
    public void ProfilePage_RefreshesNotificationStateWhenTheAppResumes()
    {
        var codeBehind = ReadSource(Page + ".cs");
        var onAppearing = Element(codeBehind, "protected override void OnAppearing()", "\n    }\n");
        var onDisappearing = Element(codeBehind, "protected override void OnDisappearing()", "\n    }\n");
        var handler = Element(codeBehind, "private void OnWindowResumed(", "\n    }\n");

        // Returning from device Settings resumes the app without raising OnAppearing.
        Assert.Contains("Resumed += OnWindowResumed", onAppearing, StringComparison.Ordinal);
        Assert.Contains("Resumed -= OnWindowResumed", onDisappearing, StringComparison.Ordinal);
        Assert.Contains("RefreshNotificationStatus()", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_DeleteAccountUsesTerracottaAndTheSharedSubtitle()
    {
        var row = Element(ReadSource(Page), "Text=\"Delete account\"", "</Grid>");

        Assert.Contains(
            "TextColor=\"{AppThemeBinding Light={StaticResource DestructiveTerracotta}, Dark={StaticResource DestructiveTerracottaDark}}\"",
            row,
            StringComparison.Ordinal);
        Assert.Contains("Text=\"{x:Static profile:ProfileText.DeleteAccountSubtitle}\"", row, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_DestructiveRowsDoNotUsePrimary()
    {
        var page = ReadSource(Page);
        var rows = page[page.IndexOf("LogoutCommand", StringComparison.Ordinal)..page.IndexOf("Text=\"{Binding StatusMessage}\"", StringComparison.Ordinal)];

        Assert.DoesNotContain("StaticResource Primary}", rows, StringComparison.Ordinal);
    }

    /// <summary>The source from <paramref name="start"/> up to and including the next <paramref name="end"/>.</summary>
    private static string Element(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' not found");
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(to >= 0, $"'{end}' not found after '{start}'");
        return source[from..(to + end.Length)];
    }

    private static string ReadSource(string repoRelativePath) => File.ReadAllText(RepoPath(repoRelativePath));

    private static string RepoPath(string repoRelativePath) =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", repoRelativePath));
}
