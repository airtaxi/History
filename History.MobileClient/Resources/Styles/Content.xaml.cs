using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls;

namespace History.MobileClient.Resources.Styles;

public partial class Content : ResourceDictionary
{
	public Content()
	{
		InitializeComponent();
	}

    private async void OnMediaContentTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as IMediaViewModel;

        IMediaViewModel fullScreenMediaViewModel;
        if (viewModel is ImageViewModel imageViewModel) fullScreenMediaViewModel = new FullScreenImageViewModel(imageViewModel);
        else if (viewModel is VideoViewModel videoViewModel) fullScreenMediaViewModel = new FullScreenVideoViewModel(videoViewModel);
        else throw new Exception("Invalid view model type.");

        var viewerPage = new FullScreenMediaViewerPage(fullScreenMediaViewModel);
        await App.PushModalAsync(viewerPage);
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

        if (parent?.BindingContext is CommentViewModel commentViewModel) commentViewModel.HandleTap();
        else if (parent?.BindingContext is PostViewModel postViewModel) await postViewModel.HandleTapAsync();
    }
}