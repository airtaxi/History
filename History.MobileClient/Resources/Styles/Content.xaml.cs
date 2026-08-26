using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using History.Commons.Enums;
using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Text;

namespace History.MobileClient.Resources.Styles;

public partial class Content : ResourceDictionary
{
    public Content() => InitializeComponent();

    private async void OnTextTypeContentsLabelLongPressed(object sender, LongPressCompletedEventArgs e)
    {
        var label = sender as Label;

        // Walk up the visual tree to find the hosting ViewModel (BaseCommentViewModel sets IsLongPressed)
        var bindingContext = label?.Parent?.BindingContext;
        var parent = label?.Parent;
        while (parent != null && bindingContext is null or TextTypeContentsViewModel or TimelineContentsViewModel)
        {
            parent = parent.Parent;
            bindingContext = parent?.BindingContext;
        }
        if (bindingContext is BaseCommentViewModel commentViewModel) commentViewModel.IsLongPressed = true;

        var viewModel = label.BindingContext as TextTypeContentsViewModel;
        var contents = viewModel.TextTypeContents;

        var builder = new StringBuilder();
        foreach(var content in contents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
            else if (content is HashtagContent hashtagContent) builder.Append($"#{hashtagContent.Tag}");
            else if (content is HyperlinkContent hyperlinkContent) builder.Append(hyperlinkContent.Url);
        }
        var text = builder.ToString().Trim();

        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        await Clipboard.Default.SetTextAsync(text);
        await Toast.Make("텍스트가 클립보드에 복사되었습니다.").Show();
    }

    // Must set TouchGesture to invoke TapGestureCommand for Comment.xaml
    // For iOS, No need to call HandleTapAsync here. HistoryCommentViewModel's HandleTapCommand will do their job.
    private async void OnTextTypeContentsLabelTouchGestureCompleted(object sender, TouchGestureCompletedEventArgs e)
    {
        var label = sender as Label;

        // Walk up the visual tree to find the hosting ViewModel (BasePostViewModel, BaseCommentViewModel, etc.)
        // With TimelineContentsTemplate, the parent chain is: Label -> DataTemplatePresenter (TextTypeContentsViewModel)
        // -> VerticalStackLayout (TimelineContentsViewModel) -> DataTemplatePresenter -> PostTemplate (HistoryPostViewModel)
        var bindingContext = label?.Parent?.BindingContext;
        var parent = label?.Parent;
        while (parent != null && bindingContext is null or TextTypeContentsViewModel or TimelineContentsViewModel)
        {
            parent = parent.Parent;
            bindingContext = parent?.BindingContext;
        }
        if (bindingContext is null) return;

#if ANDROID
        // For Android, BaseCommentViewModel's HandleTapCommand doesn't fire automatically. still needs manaual event fire
        if (bindingContext is BaseCommentViewModel commentViewModel) await commentViewModel.HandleTapAsync();
#endif
        if (bindingContext is HistoryPublicPostViewModel publicPostViewModel) await publicPostViewModel.HandleProfileTapAsync();
        else if (bindingContext is BasePostViewModel postViewModel && (postViewModel.PostType != PostType.Unwrapped || postViewModel.IsParentPost)) await postViewModel.HandleTapAsync();
    }

    private void OnTextTypeContentsLabelSizeChanged(object sender, EventArgs e)
    {
        var label = sender as Label;
        var viewModel = label.BindingContext as TextTypeContentsViewModel;
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

    private void OnWrappedMediaContentsCarouselViewSizeChanged(object sender, EventArgs e)
    {
        var carouselView = sender as CarouselView;
        if (carouselView.Clip is not RoundRectangleGeometry clip) return;

        clip.Rect = new Rect(0, 0, carouselView.Width, carouselView.Height);

        if (carouselView.BindingContext is BaseWrappedMediaContentsViewModel viewModel)
        {
            viewModel.CarouselViewWidth = -1;
            viewModel.UpdateCarouselViewSize();
        }
    }
}