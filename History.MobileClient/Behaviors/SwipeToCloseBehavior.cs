using System.Diagnostics;
using History.MobileClient.ViewModels;

#if IOS
using History.MobileClient.ContentViews;
#else
using History.MobileClient.Resources.Styles;
#endif

namespace History.MobileClient.Behaviors
{
    public class SwipeToCloseBehavior : Behavior<View>
    {
        private double _startX;
        private double _startY;
        private double _totalX;
        private double _totalY;
        private DateTime _startTime;
        private View _associatedObject;
        private bool _isPanning;
        private bool _wasFullscreenSwipeable;
        private FullScreenMediaContentViewModel _fullScreenMediaContentViewModel;
        private const double SwipeThreshold = 150;

        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);
            _associatedObject = bindable;

#if IOS
            var carouselView = bindable as CarouselView;
            if (carouselView != null) _fullScreenMediaContentViewModel = carouselView.BindingContext as FullScreenMediaContentViewModel;
            else if (bindable is not DataTemplatePresenter) return;
#else
            // CarouselView is not yet attached.
            // Fetch carousel view on next frame using Dispatcher
            Dispatcher.Dispatch(() =>
            {
                var parentCarousel = Media.FindCarouselView(bindable);
                if (parentCarousel != null) _fullScreenMediaContentViewModel = parentCarousel.BindingContext as FullScreenMediaContentViewModel;
            });
#endif
            bindable.GestureRecognizers.Add(CreatePanGesture());
        }

        private PanGestureRecognizer CreatePanGesture()
        {
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            return panGesture;
        }

        private async void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    if (_isPanning) return;

                    if (_fullScreenMediaContentViewModel != null)
                    {
                        if (_fullScreenMediaContentViewModel.CurrentMedia.IsInZoomMode) return;

                        _wasFullscreenSwipeable = _fullScreenMediaContentViewModel.CurrentMedia.FullScreenSwipeable;
                        _fullScreenMediaContentViewModel.CurrentMedia.FullScreenSwipeable = false;
                    }

                    _startY = e.TotalY;
                    _startX = e.TotalX;
                    _totalX = 0;
                    _totalY = 0;
                    _startTime = DateTime.Now;
                    _isPanning = true;

                    Debug.WriteLine($"STARTED: TotalX: {e.TotalX}, TotalY: {e.TotalY}");
                    break;

                case GestureStatus.Running:
                    if (!_isPanning) return;

                    _totalX = e.TotalX;
                    _totalY = e.TotalY;
                    Debug.WriteLine($"RUNNING: TotalX: {e.TotalX}, TotalY: {e.TotalY}");

                    if (Math.Abs(_totalX) > Math.Abs(_totalY) * 1.5)
                    {
                        _isPanning = false;
                        if (_fullScreenMediaContentViewModel != null) _fullScreenMediaContentViewModel.CurrentMedia.FullScreenSwipeable = _wasFullscreenSwipeable;
                        return;
                    }

                    if (Math.Abs(_totalY) > 20 && Math.Abs(_totalY) > Math.Abs(_totalX))
                    {
                        var root = GetRootContent();
                        if (root != null)
                        {
                            root.TranslationY = (e.TotalY - _startY) * 0.8;
                            root.Opacity = 1 - Math.Min(Math.Abs(_totalY) / SwipeThreshold, 0.3);
                        }
                    }
                    break;

                case GestureStatus.Completed:
                    if (!_isPanning) return;

                    Debug.WriteLine($"COMPLETED: TotalX: {_totalX}, TotalY: {_totalY}");

                    var finalDeltaY = _totalY;
                    var timeElapsed = (DateTime.Now - _startTime).TotalSeconds;
                    var velocity = Math.Abs(finalDeltaY) / timeElapsed;

                    var shouldClose = Math.Abs(finalDeltaY) > SwipeThreshold;

                    var root2 = GetRootContent();
                    if (shouldClose && root2 != null)
                    {
                        await CloseViewWithAnimation(root2, finalDeltaY > 0);
                    }
                    else if (root2 != null)
                    {
                        await root2.TranslateTo(0, 0, 200);
                        await root2.FadeTo(1, 200);
                    }

                    _isPanning = false;
                    if (_fullScreenMediaContentViewModel != null) _fullScreenMediaContentViewModel.CurrentMedia.FullScreenSwipeable = _wasFullscreenSwipeable;
                    break;
            }
        }

        private View GetRootContent()
        {
            var parent = _associatedObject;
            while (parent.Parent is View parentView) parent = parentView;
            return parent;
        }

        private static async Task CloseViewWithAnimation(View view, bool isDownSwipe)
        {
            var height = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
            var targetY = isDownSwipe ? height : -height;

            await Task.WhenAll(
                view.TranslateTo(0, targetY, 300, Easing.CubicIn),
                view.FadeTo(0, 300)
            );

            await App.PopAsync();
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);
        }
    }
}
