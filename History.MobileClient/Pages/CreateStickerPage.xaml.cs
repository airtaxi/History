using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using dccon.NET;
using dccon.NET.Models;
using FFImageLoading.Maui;
using History.Commons.Api.Sticker;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using InvenSticker.NET;
using InvenSticker.NET.Models;
using Microsoft.Maui.Controls.Shapes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using UraniumUI.Icons.MaterialSymbols;
using Path = System.IO.Path;

#if IOS
using NativeMedia;
#endif

namespace History.MobileClient.Pages;

public partial class CreateStickerPage : ContentPage
{
    private bool _isInForeground;
    private string _iconFileName;
    private MemoryStream _iconStream;
    private readonly Dictionary<string, MemoryStream> _assets = [];
    private const int MaxAssets = 50;

    public CreateStickerPage()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;

        foreach (var asset in _assets) asset.Value.Dispose();
        _iconStream?.Dispose();

        _assets.Clear();
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnIconGridTapped(object sender, TappedEventArgs e)
    {
        string fileName;
        byte[] bytes;

#if ANDROID
        var image = await AndroidMediaPickerHelper.PickMediaAsync(true, false);

        fileName = image.FileName;
        bytes = image.Bytes;
#else
        var request = new MediaPickRequest(1, MediaFileType.Image)
        {
            Title = "아이콘 선택"
        };

        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;

        using var file = files[0];
        using var stream = await file.OpenReadAsync();
        var memoryStream = new MemoryStream(); // Keep the stream open (do not use 'using' here)
        await stream.CopyToAsync(memoryStream);

        fileName = file.GenerateFileName();
        bytes = memoryStream.ToArray();

        memoryStream.Seek(0, SeekOrigin.Begin);
#endif

        // Validation
        var mimeType = Commons.MimeTypes.GetMimeType(fileName);
        if (!mimeType.StartsWith("image/"))
        {
            await DisplayAlertAsync("오류", "이미지 파일만 선택할 수 있습니다.", "확인");
#if IOS
            memoryStream.Dispose();
#endif
            return;
        }
        else if (mimeType.Contains("gif"))
        {
            await DisplayAlertAsync("오류", "정적 이미지만 사용 가능합니다. (움짤 불가)", "확인");
#if IOS
            memoryStream.Dispose();
#endif
            return;
        }

        _iconFileName = fileName;

        if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            _ = Toast.Make("움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();

#if ANDROID
        var memoryStream = new MemoryStream(bytes);
#endif
        _iconStream?.Dispose();
        _iconStream = memoryStream;

        IconImage.Source = ImageSource.FromStream(() => memoryStream);
        IconImage.IsVisible = true;
        IconPlaceholderImage.IsVisible = false;
    }

    private async void OnAddAssetsButtonClicked(object sender, EventArgs e)
    {
        var maxCount = MaxAssets - _assets.Count;
        if (maxCount <= 0)
        {
            await DisplayAlertAsync("오류", $"스티커 에셋은 최대 {MaxAssets}개까지 추가할 수 있습니다.", "확인");
            return;
        }

#if ANDROID
        var images = await AndroidMediaPickerHelper.PickMediasAsync(maxCount, true, false);
        if (images == null || images.Count == 0) return;

        var addedCount = 0;
        foreach (var image in images)
        {
            var mimeType = Commons.MimeTypes.GetMimeType(image.FileName);
            if (!mimeType.StartsWith("image/")) continue;

            if ((image.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || image.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) && addedCount == 0)
                _ = Toast.Make("움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();

            var memoryStream = new MemoryStream(image.GetBytes());
            _assets.Add(image.FileName, memoryStream);
            AddAssetToUI(image.FileName, memoryStream);
            addedCount++;
        }
#else
        var request = new MediaPickRequest(maxCount, MediaFileType.Image) { Title = "이미지 추가" };

        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;

        if (files.Any(x => x.Extension.Contains("webp", StringComparison.OrdinalIgnoreCase) || x.Extension.Contains("gif", StringComparison.OrdinalIgnoreCase)))
            _ = Toast.Make("움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();
            
        foreach (var file in files)
        {
            using var stream = await file.OpenReadAsync();
            var memoryStream = new MemoryStream(); // Keep the stream open (do not use 'using' here)
            await stream.CopyToAsync(memoryStream); 
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var mimeType = Commons.MimeTypes.GetMimeType(fileName);
            if (!mimeType.StartsWith("image/")) continue;

            _assets.Add(fileName, memoryStream);
            AddAssetToUI(fileName, memoryStream);
        }
#endif

        UpdateAssetCount();
    }

    private void AddAssetToUI(string fileName, MemoryStream stream)
    {
        var grid = new Grid
        {
            HeightRequest = 80,
            WidthRequest = 80,
            Margin = new Thickness(0, 0, 8, 8)
        };

        var image = new CachedImage
        {
            Source = ImageSource.FromStream(() => stream),
            Aspect = Aspect.AspectFill,
            DownsampleToViewSize = true,
            HeightRequest = 80,
            WidthRequest = 80
        };

        var deleteButton = new Border
        {
            HeightRequest = 24,
            WidthRequest = 24,
            BackgroundColor = Color.FromArgb("#808080"),
            StrokeThickness = 0,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, -4, -4, 0),
            Content = new Image
            {
                Source = new FontImageSource
                {
                    FontFamily = "MaterialSharp",
                    Glyph = MaterialSharp.Close,
                    Color = Colors.White
                },
                HeightRequest = 16,
                WidthRequest = 16
            },
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            _assets.Remove(fileName);
            stream.Dispose();

            AssetsFlexLayout.Children.Remove(grid);
            UpdateAssetCount();
        };
        deleteButton.GestureRecognizers.Add(tapGesture);

        grid.Children.Add(image);
        grid.Children.Add(deleteButton);

        AssetsFlexLayout.Children.Add(grid);
    }

    private void UpdateAssetCount() => AssetCountLabel.Text = $"({_assets.Count}/{MaxAssets})";

    private async void OnLoadExternalStickerBorderTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("스티커 불러오기", Constants.PromptCancel, null, "디시콘", "아카콘", "인벤스티커");
        if (action == null || action == Constants.PromptCancel) return;

        if (action == "디시콘") await LoadDcconStickerAsync();
        else if (action == "아카콘") await LoadArcaLiveEmoticonAsync();
        else if (action == "인벤스티커") await LoadInvenStickerAsync();
    }

    private async Task LoadDcconStickerAsync()
    {
        var url = await DisplayPromptAsync("디시콘 불러오기", "디시콘 URL을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "https://dccon.dcinside.com/#15276", -1, Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;

        var hashIndex = url.IndexOf('#');
        if (hashIndex == -1 || hashIndex == url.Length - 1 || !int.TryParse(url[(hashIndex + 1)..], out var packageIndex) || packageIndex <= 0)
        {
            await DisplayAlertAsync("오류", "올바른 디시콘 URL을 입력해주세요.", "확인");
            return;
        }

        try
        {
            using var client = new DcconClient();
            var detail = await client.GetPackageDetailAsync(packageIndex);

            // Fill only empty fields
            if (string.IsNullOrWhiteSpace(NameEntry.Text)) NameEntry.Text = detail.Title;

            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) && detail.Tags.Count > 0) CategoryEntry.Text = string.Join(", ", detail.Tags.Select(x => x.Replace("#", string.Empty)));

            if (string.IsNullOrWhiteSpace(DescriptionEditor.Text)) DescriptionEditor.Text = detail.Description;
            await App.ExecuteWithLoadingAsync(async () =>
            {
                await DownloadDcconIconAsync(client, detail);

                var addedCount = 0;
                var failedCount = 0;
                var usedFileNames = new HashSet<string>();
                foreach (var sticker in detail.Stickers)
                {
                    if (_assets.Count >= MaxAssets)
                    {
                        await DisplayAlertAsync("알림", $"스티커 에셋은 최대 {MaxAssets}개까지 추가할 수 있어 나머지는 건너뜁니다.", "확인");
                        break;
                    }

                    var fileName = GetDcconStickerFileName(sticker);
                    var suffix = 2;
                    while (!usedFileNames.Add(fileName)) fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{suffix++}.{sticker.Extension}";

                    try
                    {
                        var bytes = await client.DownloadStickerAsync(sticker);
                        var memoryStream = new MemoryStream(bytes);
                        _assets.Add(fileName, memoryStream);
                        AddAssetToUI(fileName, memoryStream);
                        addedCount++;
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        await DisplayAlertAsync("오류", $"일부 스티커를 불러오지 못했습니다: {sticker.Title} ({stickerException.Message})", "확인");
                    }
                }

                if (addedCount > 0) UpdateAssetCount();

                if (addedCount == 0 && failedCount == detail.Stickers.Count)
                {
                    await DisplayAlertAsync("오류", "디시콘 스티커를 불러오지 못했습니다.", "확인");
                    return;
                }

                if (addedCount > 0 && failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {addedCount}개를 불러왔고 {failedCount}개를 불러오지 못했습니다.", "확인");
                else if (failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {failedCount}개를 불러오지 못했습니다.", "확인");
                else if (addedCount > 0) await DisplayAlertAsync("성공", $"스티커 {addedCount}개를 불러왔습니다!", "확인");
            });
        }
        catch (Exception exception) { await DisplayAlertAsync("오류", $"디시콘을 불러오는 중 오류가 발생했습니다: {exception.Message}", "확인"); }
    }

    private async Task DownloadDcconIconAsync(DcconClient client, DcconPackageDetail detail)
    {
        try
        {
            var iconBytes = await client.DownloadStickerAsync(new DcconSticker { Path = detail.MainImagePath });
            var iconExtension = detail.Stickers.Count > 0 ? detail.Stickers[0].Extension : "png";
            var iconFileName = string.IsNullOrWhiteSpace(detail.MainImagePath) ? $"sticker_icon.{iconExtension}" : $"{detail.MainImagePath}.{iconExtension}";

            _iconFileName = iconFileName;
            _iconStream?.Dispose();
            _iconStream = new MemoryStream(iconBytes);

            var iconStreamCopy = _iconStream;
            IconImage.Source = ImageSource.FromStream(() => iconStreamCopy);
            IconImage.IsVisible = true;
            IconPlaceholderImage.IsVisible = false;

            if (iconFileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || iconFileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) _ = Toast.Make("움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();
        }
        catch (Exception exception) { _ = Toast.Make($"아이콘을 불러오지 못했습니다: {exception.Message}").Show(); }
    }

    private static string GetDcconStickerFileName(DcconSticker sticker) =>
        $"{(!string.IsNullOrWhiteSpace(sticker.Path) ? sticker.Path : $"sticker_{sticker.SortNumber}")}.{sticker.Extension}";

    private async Task LoadInvenStickerAsync()
    {
        var url = await DisplayPromptAsync("인벤 스티커 불러오기", "인벤 스티커 URL을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "https://imart.inven.co.kr/shop/sticker/1164", -1, Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;

        var packageIdMatch = Regex.Match(url, @"/shop/sticker/(\d+)");
        if (!int.TryParse(packageIdMatch.Groups[1].Value, out var packageId) || packageId <= 0)
        {
            await DisplayAlertAsync("오류", "올바른 인벤 스티커 URL을 입력해주세요.", "확인");
            return;
        }

        try
        {
            using var client = new InvenStickerClient();
            var detail = await client.GetDetailAsync(packageId);

            // Fill only empty fields
            if (string.IsNullOrWhiteSpace(NameEntry.Text)) NameEntry.Text = detail.Title;

            if (string.IsNullOrWhiteSpace(CategoryEntry.Text) && detail.Tags.Count > 0) CategoryEntry.Text = string.Join(", ", detail.Tags.Select(x => x.Replace("#", string.Empty)));

            await App.ExecuteWithLoadingAsync(async () =>
            {
                await DownloadInvenStickerIconAsync(client, detail);

                var addedCount = 0;
                var failedCount = 0;
                for (var index = 0; index < detail.Images.Count; index++)
                {
                    if (_assets.Count >= MaxAssets)
                    {
                        await DisplayAlertAsync("알림", $"스티커 에셋은 최대 {MaxAssets}개까지 추가할 수 있어 나머지는 건너뜁니다.", "확인");
                        break;
                    }

                    var fileName = InvenStickerFileNameHelper.GetStickerFileName(detail.Images[index], index);

                    try
                    {
                        var bytes = await client.DownloadImageAsync(detail.Images[index]);
                        var memoryStream = new MemoryStream(bytes);
                        _assets.Add(fileName, memoryStream);
                        AddAssetToUI(fileName, memoryStream);
                        addedCount++;
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        await DisplayAlertAsync("오류", $"일부 스티커를 불러오지 못했습니다: {fileName} ({stickerException.Message})", "확인");
                    }
                }

                if (addedCount > 0) UpdateAssetCount();

                if (addedCount == 0 && failedCount == detail.Images.Count)
                {
                    await DisplayAlertAsync("오류", "인벤 스티커를 불러오지 못했습니다.", "확인");
                    return;
                }

                if (addedCount > 0 && failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {addedCount}개를 불러왔고 {failedCount}개를 불러오지 못했습니다.", "확인");
                else if (failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {failedCount}개를 불러오지 못했습니다.", "확인");
                else await DisplayAlertAsync("성공", $"스티커 {addedCount}개를 불러왔습니다!", "확인");
            });
        }
        catch (Exception exception) { await DisplayAlertAsync("오류", $"인벤 스티커를 불러오는 중 오류가 발생했습니다: {exception.Message}", "확인"); }
    }

    private async Task DownloadInvenStickerIconAsync(InvenStickerClient client, InvenStickerPackageDetail detail)
    {
        try
        {
            var iconBytes = await client.DownloadImageAsync(new InvenStickerImage { Url = detail.ThumbnailUrl });
            var iconExtension = detail.Images.Count > 0 ? detail.Images[0].Extension : "png";
            var iconFileName = $"sticker_icon.{iconExtension}";

            _iconFileName = iconFileName;
            _iconStream?.Dispose();
            _iconStream = new MemoryStream(iconBytes);

            var iconStreamCopy = _iconStream;
            IconImage.Source = ImageSource.FromStream(() => iconStreamCopy);
            IconImage.IsVisible = true;
            IconPlaceholderImage.IsVisible = false;

            if (iconFileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || iconFileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)) _ = Toast.Make("움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();
        }
        catch (Exception exception) { _ = Toast.Make($"아이콘을 불러오지 못했습니다: {exception.Message}").Show(); }
    }

    private async Task LoadArcaLiveEmoticonAsync()
    {
        var url = await DisplayPromptAsync("아카콘 불러오기", "아카콘 URL을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "https://arca.live/e/52863", -1, Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;

        var emoticonIndex = Regex.Match(url, @"/(?:e|emoticon)/(\d+)").Groups[1].Value;
        if (!int.TryParse(emoticonIndex, out var emoticonId) || emoticonId <= 0)
        {
            await DisplayAlertAsync("오류", "올바른 아카콘 URL을 입력해주세요.", "확인");
            return;
        }
        
        try
        {
            await App.ExecuteWithLoadingAsync(async () =>
            {
                var stickers = await FetchArcaLiveEmoticonsAsync(emoticonId);
                if (stickers.Count == 0)
                {
                    await DisplayAlertAsync("오류", "아카콘 스티커를 불러오지 못했습니다.", "확인");
                    return;
                }

                var addedCount = 0;
                var failedCount = 0;
                var usedFileNames = new HashSet<string>();
                using var httpClient = new HttpClient();
                foreach (var stickerNode in stickers)
                {
                    if (_assets.Count >= MaxAssets)
                    {
                        await DisplayAlertAsync("알림", $"스티커 에셋은 최대 {MaxAssets}개까지 추가할 수 있어 나머지는 건너뜁니다.", "확인");
                        break;
                    }

                    var imageUrl = stickerNode["imageUrl"]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                    var fileName = GetArcaLiveEmoticonFileName(imageUrl);
                    var suffix = 2;
                    while (!usedFileNames.Add(fileName)) fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{suffix++}{Path.GetExtension(fileName)}";

                    try
                    {
                        var bytes = await httpClient.GetByteArrayAsync(imageUrl);
                        var memoryStream = new MemoryStream(bytes);
                        _assets.Add(fileName, memoryStream);
                        AddAssetToUI(fileName, memoryStream);
                        addedCount++;
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        await DisplayAlertAsync("오류", $"일부 스티커를 불러오지 못했습니다: {fileName} ({stickerException.Message})", "확인");
                    }
                }

                if (addedCount > 0) UpdateAssetCount();

                if (addedCount > 0 && failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {addedCount}개를 불러왔고 {failedCount}개를 불러오지 못했습니다.", "확인");
                else if (failedCount > 0) await DisplayAlertAsync("알림", $"스티커 {failedCount}개를 불러오지 못했습니다.", "확인");
                else await DisplayAlertAsync("성공", $"스티커 {addedCount}개를 불러왔습니다!", "확인");
            });
        }
        catch (Exception exception) { await DisplayAlertAsync("오류", $"아카콘을 불러오는 중 오류가 발생했습니다: {exception.Message}", "확인"); }
    }

    private static async Task<JsonArray> FetchArcaLiveEmoticonsAsync(int emoticonId)
    {
        using var httpClient = new HttpClient();
        var json = await httpClient.GetStringAsync($"https://arca.live/api/emoticon/{emoticonId}");
        return JsonNode.Parse(json)?.AsArray() ?? [];
    }

    private static string GetArcaLiveEmoticonFileName(string imageUrl)
    {
        var urlWithoutQuery = imageUrl.Split('?')[0];
        var fileName = Path.GetFileName(urlWithoutQuery);
        return string.IsNullOrWhiteSpace(fileName) ? $"arcalive_{new Guid().ToString()[..8]}.png" : fileName;
    }

    private async void OnCreateButtonClicked(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(_iconFileName))
        {
            await DisplayAlertAsync("오류", "아이콘을 선택해주세요.", "확인");
            return;
        }

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("오류", "스티커 이름을 입력해주세요.", "확인");
            return;
        }

        if (string.IsNullOrWhiteSpace(CategoryEntry.Text))
        {
            await DisplayAlertAsync("오류", "카테고리를 입력해주세요.", "확인");
            return;
        }

        if (_assets.Count == 0)
        {
            await DisplayAlertAsync("오류", "스티커 에셋을 최소 1개 이상 추가해주세요.", "확인");
            return;
        }

        CreateButton.IsEnabled = false;
        try
        {
            // Read files
            var iconBytes = _iconStream.ToArray();

            var assetFiles = new Dictionary<string, byte[]>();
            foreach (var asset in _assets) assetFiles[asset.Key] = asset.Value.ToArray();

            var result = await App.ExecuteRequestAsync(new CreateSticker(
                NameEntry.Text.Trim(),
                CategoryEntry.Text.Trim(),
                DescriptionEditor.Text?.Trim(),
                PrivateSwitch.IsToggled,
                iconBytes,
                _iconFileName,
                assetFiles
            ));

            if (result.IsSuccess)
            {
                await DisplayAlertAsync("성공", "스티커가 생성되었습니다!", "확인");
                await App.PopAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("오류", $"스티커 생성 중 오류가 발생했습니다: {ex.Message}", "확인");
        }
        finally { CreateButton.IsEnabled = true; }
    }
}
