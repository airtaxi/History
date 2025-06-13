using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Content : ResourceDictionary
{
	public Content()
	{
		InitializeComponent();
	}

    private async void OnTextAndProfileContentsLabelLongPressed(object sender, LongPressCompletedEventArgs e)
    {
        var label = sender as Label;
        var texts = label.FormattedText.Spans.SelectMany(x => x.Text);
        var text = string.Concat(texts);

        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        await Clipboard.Default.SetTextAsync(text);
        await Toast.Make("텍스트가 클립보드에 복사되었습니다.").Show();
    }

    private async void OnTextAndProfileContentsLabelTapped(object sender, TappedEventArgs e)
    {
        var label = sender as Label;
        var parent = label.Parent;
        if (parent?.BindingContext is null) return;

        if (parent.BindingContext is CommentViewModel commentViewModel) await commentViewModel.HandleTapAsync();
        else if (parent.BindingContext is PostViewModel postViewModel && (postViewModel.IsTimeline || postViewModel.IsParentPost)) await postViewModel.HandleTapAsync();
    }

    private void OnTextAndProfileContentsLabelSizeChanged(object sender, EventArgs e)
    {
        var label = sender as Label;
        var viewModel = label.BindingContext as TextAndProfileContentsViewModel;
        Application.Current.Dispatcher.Dispatch(() =>
        {
            Application.Current.Dispatcher.Dispatch(() =>
            {
                var lineHeight = label.LineHeight;
                label.LineHeight = lineHeight + 1;
                label.LineHeight = lineHeight;
            });
        });
    }

    private void OnWrappedMediaContentsCarouselViewCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
    {
        var carouselView = sender as CarouselView;
        if (carouselView.CurrentItem is not MediaContentViewModel mediaContentViewModel) return;

        if (!mediaContentViewModel.IsTimeline)
        {
            Debug.WriteLine($"CarouselViewCurrentItemChanged: {carouselView.Position} / {mediaContentViewModel.Media.Uri}");
            WeakReferenceMessenger.Default.Send(new ResizeMediaCarouselViewMessage(mediaContentViewModel.Media));
        }

#if IOS
            carouselView.ScrollTo(carouselView.Position, animate: false);
#endif
    }

    private void OnWrappedMediaContentsCarouselViewLoaded(object sender, EventArgs e)
    {
        var carouselView = sender as CarouselView;
        if (carouselView.BindingContext is not WrappedMediaContentsViewModel viewModel) return;

        var firstMedia = viewModel.FirstMedia;

        if (!firstMedia.IsTimeline)
        {
            carouselView.Dispatcher.Dispatch(() =>
            {
                WeakReferenceMessenger.Default.Send(new ResizeMediaCarouselViewMessage(firstMedia.Media));
            });
        }
    }

    private void OnWrappedMediaContentsCarouselViewSizeChanged(object sender, EventArgs e)
    {
        var carouselView = sender as CarouselView;
        if (carouselView.Clip is not RoundRectangleGeometry clip) return;

        clip.Rect = new Rect(0, 0, carouselView.Width, carouselView.Height);
    }
}