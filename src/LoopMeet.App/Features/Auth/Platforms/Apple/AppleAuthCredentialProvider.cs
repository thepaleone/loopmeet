#if IOS || MACCATALYST
using AuthenticationServices;
using Foundation;

namespace LoopMeet.App.Features.Auth.Platforms.Apple;

internal static class AppleAuthCredentialProvider
{
    /// <summary>
    /// Presents the native Sign in with Apple sheet. Returns the granted
    /// credential, or null if the user cancels. Throws when Apple's
    /// authorization controller reports a non-cancel error.
    /// </summary>
    public static Task<ASAuthorizationAppleIdCredential?> RequestCredentialAsync(string hashedNonce)
    {
        var tcs = new TaskCompletionSource<ASAuthorizationAppleIdCredential?>();

        var provider = new ASAuthorizationAppleIdProvider();
        var request = provider.CreateRequest();
        request.RequestedScopes = new[] { ASAuthorizationScope.Email, ASAuthorizationScope.FullName };
        request.Nonce = hashedNonce;

        var controller = new ASAuthorizationController(new[] { request });
        var bridge = new ControllerBridge(tcs);
        controller.Delegate = bridge;
        controller.PresentationContextProvider = bridge;
        controller.PerformRequests();

        // Keep the bridge alive until the TCS completes so the delegate
        // callbacks land on a live managed object.
        tcs.Task.ContinueWith(_ => GC.KeepAlive(bridge), TaskScheduler.Default);

        return tcs.Task;
    }

    private sealed class ControllerBridge : NSObject, IASAuthorizationControllerDelegate, IASAuthorizationControllerPresentationContextProviding
    {
        private readonly TaskCompletionSource<ASAuthorizationAppleIdCredential?> _tcs;

        public ControllerBridge(TaskCompletionSource<ASAuthorizationAppleIdCredential?> tcs)
        {
            _tcs = tcs;
        }

        [Export("authorizationController:didCompleteWithAuthorization:")]
        public void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
        {
            if (authorization.GetCredential<ASAuthorizationAppleIdCredential>() is { } credential)
            {
                _tcs.TrySetResult(credential);
                return;
            }

            _tcs.TrySetResult(null);
        }

        [Export("authorizationController:didCompleteWithError:")]
        public void DidComplete(ASAuthorizationController controller, NSError error)
        {
            if (error.Code == (long)ASAuthorizationError.Canceled)
            {
                _tcs.TrySetResult(null);
                return;
            }

            _tcs.TrySetException(new Exception($"Apple authorization failed: {error.LocalizedDescription} (code {error.Code})"));
        }

        [Export("presentationAnchorForAuthorizationController:")]
        public UIKit.UIWindow GetPresentationAnchor(ASAuthorizationController controller)
        {
            var scenes = UIKit.UIApplication.SharedApplication.ConnectedScenes;
            foreach (var scene in scenes)
            {
                if (scene is UIKit.UIWindowScene windowScene && windowScene.ActivationState == UIKit.UISceneActivationState.ForegroundActive)
                {
                    foreach (var window in windowScene.Windows)
                    {
                        if (window.IsKeyWindow) return window;
                    }
                    if (windowScene.Windows.Length > 0) return windowScene.Windows[0];
                }
            }

            return new UIKit.UIWindow();
        }
    }
}
#endif
