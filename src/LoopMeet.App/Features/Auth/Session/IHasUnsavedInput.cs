namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// Implemented by form viewmodels so a forced sign-out can tell the user their
/// in-progress input was lost (clarification Q4). Checked on the main thread only.
/// </summary>
public interface IHasUnsavedInput
{
    bool HasUnsavedInput { get; }
}
