using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.WinUI;
using History.Uno.ViewModels;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.Uno.Styles;

public partial class Content
{
    public Content()
    {
        InitializeComponent();
    }

    public async void OnWrappedMediaFlipViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not FlipView filpView) return;

        var selectedItem = filpView.SelectedItem as MediaContentViewModel;
        var selectedImageViewModel = selectedItem?.Media as ImageViewModel;

        var children = filpView.FindChildren().OfType<Image>();
        var image = children.FirstOrDefault(x => (x.DataContext as ImageViewModel) == selectedImageViewModel);
        if (image == null) await App.DisplayAlertAsync("오류", "image null");

        if (image.Source is not BitmapSource bitmap || bitmap.PixelHeight <= 0) return;
        filpView.Height = bitmap.PixelHeight;
    }
}
