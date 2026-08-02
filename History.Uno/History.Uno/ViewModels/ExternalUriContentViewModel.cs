using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace History.Uno.ViewModels;

public partial class ExternalUrlContentViewModel(ExternalUrlContent externalUrlContent) : ObservableObject, IContentViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Description))]
    [NotifyPropertyChangedFor(nameof(Domain))]
    [NotifyPropertyChangedFor(nameof(ThumbnailImage))]
    public partial ExternalUrlContent ExternalUrlContent { get; set; } = externalUrlContent;

    public string Title => ExternalUrlContent.Title;
    public string Description => ExternalUrlContent.Description;
    public string Domain => ExternalUrlContent.Domain;
    public ImageViewModel ThumbnailImage => new(ExternalUrlContent.ThumbnailImageUrl) { Stretch = Stretch.UniformToFill };

    [RelayCommand]
    public async Task HandleTapAsync() => await Launcher.LaunchUriAsync(new Uri(ExternalUrlContent.SourceUrl));

    [RelayCommand]
    public async Task HandleLongPressAsync()
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(ExternalUrlContent.SourceUrl);
        Clipboard.SetContent(dataPackage);
        await App.DisplayAlertAsync("안내", "링크가 클립보드에 복사되었습니다.", Constants.PromptOk);
    }
}
