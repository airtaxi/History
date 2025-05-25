using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace History.MobileClient.ViewModels;

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
    public ImageViewModel ThumbnailImage => new(ExternalUrlContent.ThumbnailImageUrl) { Aspect = Aspect.AspectFill };

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        var uri = new Uri(ExternalUrlContent.SourceUrl);
        await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
    }

    [RelayCommand]
    public async Task HandleLongPressAsync()
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        await Clipboard.Default.SetTextAsync(ExternalUrlContent.SourceUrl);
        await Toast.Make("링크가 클립보드에 복사되었습니다.").Show();
    }
}
