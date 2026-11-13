namespace LoopMeet.App.Features.Profile;

/// <summary>
/// When the Profile page uses one column or two, and how wide its content may grow (feature 022).
/// MAUI-free so it can be unit tested: keep Microsoft.Maui.* out of this file.
/// </summary>
public static class ProfileLayout
{
    /// <summary>From this page width (points), the header card moves into a left column.</summary>
    public const double TwoColumnMinWidth = 800;

    public static bool UsesTwoColumns(double width) => width >= TwoColumnMinWidth;

    /// <summary>Caps the content so rows stay readable in landscape and on tablets.</summary>
    public static double MaxContentWidth(bool twoColumns) => twoColumns ? 1100 : 600;
}
