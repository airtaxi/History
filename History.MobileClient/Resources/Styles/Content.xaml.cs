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

        // Walk up the visual tree to find the hosting ViewModel (CommentViewModel sets IsLongPressed)
        var bindingContext = label?.Parent?.BindingContext;
        var parent = label?.Parent;
        while (parent != null && bindingContext is null or TextTypeContentsViewModel or TimelineContentsViewModel)
        {
            parent = parent.Parent;
            bindingContext = parent?.BindingContext;
        }
        if (bindingContext is CommentViewModel commentViewModel) commentViewModel.IsLongPressed = true;

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
    // For iOS, No need to call HandleTapAsync here. CommentViewModel's HandleTapCommand will do their job.
    private async void OnTextTypeContentsLabelTouchGestureCompleted(object sender, TouchGestureCompletedEventArgs e)
    {
        var label = sender as Label;

        // Walk up the visual tree to find the hosting ViewModel (PostViewModel, CommentViewModel, etc.)
        // With TimelineContentsTemplate, the parent chain is: Label -> DataTemplatePresenter (TextTypeContentsViewModel)
        // -> VerticalStackLayout (TimelineContentsViewModel) -> DataTemplatePresenter -> PostTemplate (PostViewModel)
        var bindingContext = label?.Parent?.BindingContext;
        var parent = label?.Parent;
        while (parent != null && bindingContext is null or TextTypeContentsViewModel or TimelineContentsViewModel)
        {
            parent = parent.Parent;
            bindingContext = parent?.BindingContext;
        }
        if (bindingContext is null) return;

#if ANDROID
        // For Android, CommentViewModel's HandleTapCommand doesn't fire automatically. still needs manaual event fire
        if (bindingContext is CommentViewModel commentModel) await commentModel.HandleTapAsync();
#endif
        if (bindingContext is PublicPostViewModel publicPostViewModel) await publicPostViewModel.HandleProfileTapAsync();
        else if (bindingContext is PostViewModel postViewModel && (postViewModel.PostType != Enums.PostType.Unwrapped || postViewModel.IsParentPost)) await postViewModel.HandleTapAsync();
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