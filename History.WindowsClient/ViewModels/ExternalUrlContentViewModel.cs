using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.TimeLineData;
using Windows.ApplicationModel.DataTransfer;

namespace History.WindowsClient.ViewModels;

// External URL preview card surface. The control owns a single instance and
// pushes data in through the Update overloads.
public sealed partial class ExternalUrlContentViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(Description))]
    [NotifyPropertyChangedFor(nameof(Domain))]
    [NotifyPropertyChangedFor(nameof(ThumbnailImageSource))]
    public partial ExternalUrlContent ExternalUrlContent { get; private set; }

    public string Title => ExternalUrlContent?.Title;
    public string Description => ExternalUrlContent?.Description;
    public string Domain => ExternalUrlContent?.Domain;
    public BitmapImage ThumbnailImageSource => string.IsNullOrEmpty(ExternalUrlContent?.ThumbnailImageUrl) ? null : new BitmapImage(new Uri(ExternalUrlContent.ThumbnailImageUrl));

    public void Update(ExternalUrlContent externalUrlContent) => ExternalUrlContent = externalUrlContent;

    // Kakao Story scrap overload — maps the scrap card onto the same surface.
    public void Update(Scrap scrap) => Update(new ExternalUrlContent
    {
        Title = scrap.title,
        Description = scrap.description,
        SourceUrl = scrap.dest_url ?? scrap.url,
        ThumbnailImageUrl = scrap.image?.FirstOrDefault()
    });

    [RelayCommand]
    private async Task OpenLinkAsync()
    {
        var sourceUrl = ExternalUrlContent?.SourceUrl;
        if (!Uri.IsWellFormedUriString(sourceUrl, UriKind.Absolute)) return;

        // TODO: Navigate to the in-app post/profile page for internal links once implemented (Utils.OpenLinkAsync parity).
        await Windows.System.Launcher.LaunchUriAsync(new Uri(sourceUrl));
    }

    [RelayCommand]
    private void CopyLink()
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(ExternalUrlContent?.SourceUrl ?? string.Empty);
        Clipboard.SetContent(dataPackage);
    }
}