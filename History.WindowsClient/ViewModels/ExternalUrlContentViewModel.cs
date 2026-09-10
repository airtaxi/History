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
    public BitmapImage ThumbnailImageSource
    {
        get
        {
            var thumbnailImageUrl = ExternalUrlContent?.ThumbnailImageUrl;
            if (string.IsNullOrEmpty(thumbnailImageUrl)) return null;

            // Protocol-relative URLs (//host/path) are rejected by BitmapImage's URI factory,
            // so adopt the https scheme before parsing.
            if (thumbnailImageUrl.StartsWith("//")) thumbnailImageUrl = "https:" + thumbnailImageUrl;

            if (!Uri.TryCreate(thumbnailImageUrl, UriKind.Absolute, out var thumbnailUri)) return null;
            if (thumbnailUri.Scheme is not ("http" or "https")) return null;

            try { return new BitmapImage(thumbnailUri); }
            catch { return null; }
        }
    }

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

        await Utils.OpenLinkAsync(sourceUrl);
    }

    [RelayCommand]
    private void CopyLink()
    {
        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(ExternalUrlContent?.SourceUrl ?? string.Empty);
        Clipboard.SetContent(dataPackage);
    }
}