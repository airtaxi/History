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

    private async void OnMediaCarouselViewContentTapped(object sender, TappedEventArgs e)
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
}