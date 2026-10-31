using Foundation;

namespace LoopMeet.App;

// Required by the Xcode 27 SDK (UIScene lifecycle). MAUI's base class creates the
// window and forwards scene events; AppDelegate stays the application delegate.
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
