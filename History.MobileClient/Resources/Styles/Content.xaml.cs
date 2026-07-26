using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Content : ResourceDictionary
{
    public Content() => InitializeComponent();

    private async void OnTextTypeContentsLabelLongPressed(object sender, LongPressCompletedEventArgs e)
    {
        var label = sender as Label;

        // See OnTextTypeContentsLabelTouchGestureCompleted method for why this needed
        if (label.Parent?.BindingContext is CommentViewModel commentViewModel) commentViewModel.IsLongPressed = true;

        var viewModel = label.BindingContext as TextTypeContentsViewModel;
        var contents = viewModel.TextTypeContents;

        var builder = new StringBuilder();
        foreach(var content in contents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
            else if (content is HashtagContent hashtagContent) builder.Append($"#{hashtagContent.Tag}");
        }
        var text = builder.ToString().Trim();

        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        await Clipboard.Default.SetTextAsync(text);
        await Toast.Make("텍스트가 클립보드에 복사되었습니다.").Show();
    }

    // Must set TouchGesture to invoke TapGestureCommand for Comment.xaml
    // No need to call HandleTapAsync here. CommentViewModel's HandleTapCommand will do their job.
    private async void OnTextTypeContentsLabelTouchGestureCompleted(object sender, TouchGestureCompletedEventArgs e)
    {
        var label = sender as Label;
        var parent = label.Parent;
        if (parent?.BindingContext is null) return;

        if (parent.BindingContext is PublicPostViewModel publicPostViewModel) await publicPostViewModel.HandleProfileTapAsync();
        else if (parent.BindingContext is PostViewModel postViewModel && (postViewModel.PostType != Enums.PostType.Unwrapped || postViewModel.IsParentPost)) await postViewModel.HandleTapAsync();
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

        if (carouselView.BindingContext is WrappedMediaContentsViewModel viewModel)
        {
            viewModel.CarouselViewWidth = -1;
            viewModel.UpdateCarouselViewSize();
        }
    }
}