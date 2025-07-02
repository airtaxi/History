using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.Contents;
using History.MobileClient.DataTypes;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;
using System.Text;
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
        var viewModel = label.BindingContext as TextAndProfileContentsViewModel;
        var contents = viewModel.TextAndProfileContents;

        var builder = new StringBuilder();
        foreach(var content in contents)
        {
            if (content is TextContent textContent) builder.Append(textContent.Text);
            else if (content is ProfileContent profileContent) builder.Append(profileContent.Nickname);
        }
        var text = builder.ToString().Trim();

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
        else if (parent.BindingContext is PublicPostViewModel publicPostViewModel) await publicPostViewModel.HandleProfileTapAsync();
        else if (parent.BindingContext is PostViewModel postViewModel && (postViewModel.PostType != Enums.PostType.Unwrapped || postViewModel.IsParentPost)) await postViewModel.HandleTapAsync();
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

    private void OnWrappedMediaContentsCarouselViewSizeChanged(object sender, EventArgs e)
    {
        var carouselView = sender as CarouselView;
        if (carouselView.Clip is not RoundRectangleGeometry clip) return;

        clip.Rect = new Rect(0, 0, carouselView.Width, carouselView.Height);

        var viewModel = carouselView.BindingContext as WrappedMediaContentsViewModel;
        viewModel.UpdateCarouselViewHeight();
    }

    private void OnWrappedMediaContentsCarouselViewPositionChanged(object sender, PositionChangedEventArgs e)
    {
        var carouselView = sender as CarouselView;
        carouselView.ScrollTo(carouselView.Position, animate: false);
    }
}