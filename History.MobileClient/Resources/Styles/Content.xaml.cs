using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;

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

        if (parent?.BindingContext is CommentViewModel commentViewModel) commentViewModel.HandleTap();
        else if (parent?.BindingContext is PostViewModel postViewModel && postViewModel.WrapMedias) await postViewModel.HandleTapAsync();
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
}