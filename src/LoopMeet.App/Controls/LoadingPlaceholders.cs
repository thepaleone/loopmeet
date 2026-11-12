namespace LoopMeet.App.Controls;

/// <summary>
/// When a section shows SkeletonList placeholders: only while it has never finished a load and has
/// nothing to display. A reload therefore never covers cards that are already listed, and a
/// finished load (with items, empty or failed) always ends them. MAUI-free so it can be unit tested.
/// </summary>
public static class LoadingPlaceholders
{
    public static bool ShouldShow(bool hasCompletedLoad, int itemCount) => !hasCompletedLoad && itemCount == 0;
}
