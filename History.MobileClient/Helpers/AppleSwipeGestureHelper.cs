#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Helpers;

public class AppleSwipeGestureHelper
{
    private readonly UIKit.UIViewController _modalViewController;
    private readonly UIKit.UIView _containerView;

    private AppleSwipeGestureHelper(Page page)
    {
        if (page.Handler?.PlatformView is UIKit.UIView platformView)
        {
            _modalViewController = GetModalViewController(platformView);
            _containerView = _modalViewController?.View ?? platformView;

            var panGesture = new UIKit.UIPanGestureRecognizer(HandlePanGesture);
            if (platformView.GestureRecognizers == null || platformView.GestureRecognizers.Length == 0)
                platformView.AddGestureRecognizer(panGesture);
        }
    }

    public static void ApplyToPage(Page page) => _ = new AppleSwipeGestureHelper(page);

    private static UIKit.UIViewController GetModalViewController(UIKit.UIView view)
    {
        UIKit.UIResponder responder = view;
        while (responder != null)
        {
            if (responder is UIKit.UIViewController controller) return controller;
            responder = responder.NextResponder;
        }
        return null;
    }

    private void HandlePanGesture(UIKit.UIPanGestureRecognizer gesture)
    {
        if (_containerView == null) return;

        var translation = gesture.TranslationInView(gesture.View);
        var velocity = gesture.VelocityInView(gesture.View);

        switch (gesture.State)
        {
            case UIKit.UIGestureRecognizerState.Changed:
                var translationX = ObjCRuntime.NMath.Max(0, translation.X);
                var resistance = 1.0f - ObjCRuntime.NMath.Min(0.7f, translationX / 400.0f);
                var adjustedTranslation = translationX * resistance;

                _containerView.Transform = CoreGraphics.CGAffineTransform.MakeTranslation(adjustedTranslation, 0);
                break;

            case UIKit.UIGestureRecognizerState.Ended:
            case UIKit.UIGestureRecognizerState.Cancelled:
                bool shouldDismiss = translation.X > 120 || velocity.X > 800;

                if (shouldDismiss)
                {
                    UIKit.UIView.Animate(0.2, 0, UIKit.UIViewAnimationOptions.CurveEaseOut, () =>
                    {
                        _containerView.Transform = CoreGraphics.CGAffineTransform.MakeTranslation(_containerView.Frame.Width, 0);
                    }, async () =>
                    {
                        await App.PopModalAsync();
                    });
                }
                else
                {
                    UIKit.UIView.Animate(0.2, 0, UIKit.UIViewAnimationOptions.CurveEaseOut, () =>
                    {
                        _containerView.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
                    }, null);
                }
                break;
        }
    }
}

#endif