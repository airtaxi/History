using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dccon.NET;
using dccon.NET.Models;
using History.Commons;
using History.Commons.Api.Sticker;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using InvenSticker.NET;
using InvenSticker.NET.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels.Sticker;

// Sticker create window state: icon/asset picking, external sticker imports (DCCon/Arca/Inven),
// form validation, and the create request that closes the window with a success flag.
public sealed partial class CreateStickerWindowViewModel : BaseViewModel
{
    private const int MaxAssetCount = 200;
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly string[] ExternalImportOptions = ["디시콘", "아카콘", "인벤스티커"];

    private byte[] _iconBytes;
    private string _iconFileName;

    // Raised when the sticker was created successfully, so the hosting window closes itself.
    public event EventHandler Created;

    // Whether the create request succeeded, so the hosted sticker list refreshes when the
    // window closes.
    public bool HasCreated { get; private set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Category { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPrivate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IconVisibility))]
    [NotifyPropertyChangedFor(nameof(IconPlaceholderVisibility))]
    public partial BitmapImage IconSource { get; set; }

    public ObservableCollection<CreateStickerAssetItemViewModel> Assets { get; } = [];

    public Visibility IconVisibility => IconSource == null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility IconPlaceholderVisibility => IconSource == null ? Visibility.Visible : Visibility.Collapsed;

    public string AssetCountText => $"({Assets.Count}/{MaxAssetCount})";

    public CreateStickerWindowViewModel() => Assets.CollectionChanged += OnAssetsCollectionChanged;

    // Removes the asset tile from the draft.
    public void RemoveAsset(CreateStickerAssetItemViewModel asset) => Assets.Remove(asset);

    // Picks the sticker icon; the icon must be a static image under the size limit.
    [RelayCommand]
    private async Task PickIconAsync()
    {
        var result = await PickImageAsync("아이콘 선택");
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        if (!IsImageFile(fileName))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "이미지 파일만 선택할 수 있습니다."));
            return;
        }

        if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "정적 이미지만 사용 가능합니다. (움짤 불가)"));
            return;
        }

        var bytes = await File.ReadAllBytesAsync(result.Path);
        if (bytes.Length > MaxFileSize)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"아이콘은 {MaxFileSize / 1024 / 1024}MB 이하의 이미지만 사용할 수 있습니다."));
            return;
        }

        _iconFileName = fileName;
        _iconBytes = bytes;
        IconSource = await CreateBitmapImageAsync(bytes);
    }

    // Adds the picked images as sticker assets: the remaining slots cap the count, and files
    // that are not images or exceed the size limit are skipped and reported once.
    [RelayCommand]
    private async Task AddAssetsAsync()
    {
        if (Assets.Count >= MaxAssetCount)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("스티커 에셋", $"스티커 에셋은 최대 {MaxAssetCount}개까지 추가할 수 있습니다."));
            return;
        }

        var results = await PickFilesAsync(new FileOpenPickerParameters(Constants.ImageFileTypeFilters, PickerLocationId.PicturesLibrary, "이미지 추가"));
        if (results == null || results.Count == 0) return;

        var skippedCount = 0;
        foreach (var result in results)
        {
            if (Assets.Count >= MaxAssetCount)
            {
                skippedCount++;
                continue;
            }

            var fileName = Path.GetFileName(result.Path);
            if (!IsImageFile(fileName))
            {
                skippedCount++;
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(result.Path);
            if (bytes.Length > MaxFileSize)
            {
                skippedCount++;
                continue;
            }

            Assets.Add(new CreateStickerAssetItemViewModel(BuildUniqueFileName(fileName), bytes, await CreateBitmapImageAsync(bytes), this));
        }

        if (skippedCount > 0) await ShowMessageDialogAsync(new MessageDialogParameters("스티커 에셋", $"{skippedCount}개 파일이 제외되었습니다. 각 파일은 {MaxFileSize / 1024 / 1024}MB 이하의 이미지만 추가할 수 있습니다."));
    }

    // Shows the external sticker import action list and runs the loader for the chosen entry.
    [RelayCommand]
    private async Task ImportExternalAsync()
    {
        var action = await ShowSelectionDialogAsync("스티커 불러오기", ExternalImportOptions);
        if (action == null) return;

        if (action == "디시콘") await ImportDcconAsync();
        else if (action == "아카콘") await ImportArcaLiveEmoticonAsync();
        else if (action == "인스티커") await ImportInvenStickerAsync();
    }

    // Imports a DCCon package: the URL fragment carries the package index, and the fetched
    // package title, tags, and description fill only the empty form fields.
    private async Task ImportDcconAsync()
    {
        var url = await ShowInputDialogAsync(new InputDialogParameters("디시콘 불러오기", "디시콘 URL을 입력해주세요.", "https://dccon.dcinside.com/#15276", showCancel: true));
        if (string.IsNullOrWhiteSpace(url)) return;

        var hashIndex = url.IndexOf('#');
        if (hashIndex == -1 || hashIndex == url.Length - 1 || !int.TryParse(url[(hashIndex + 1)..], out var packageIndex) || packageIndex <= 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "올바른 디시콘 URL을 입력해주세요."));
            return;
        }

        try
        {
            using var client = new DcconClient();
            var detail = await client.GetPackageDetailAsync(packageIndex);

            if (string.IsNullOrWhiteSpace(Name)) Name = detail.Title;
            if (string.IsNullOrWhiteSpace(Category) && detail.Tags.Count > 0) Category = string.Join(", ", detail.Tags.Select(x => x.Replace("#", string.Empty)));
            if (string.IsNullOrWhiteSpace(Description)) Description = detail.Description;

            await ExecuteWithLoadingAsync(async () =>
            {
                await TrySetDcconIconAsync(client, detail);

                var addedCount = 0;
                var failedCount = 0;
                List<string> failureMessages = [];
                foreach (var sticker in detail.Stickers)
                {
                    if (Assets.Count >= MaxAssetCount)
                    {
                        await ShowMessageDialogAsync(new MessageDialogParameters("알림", $"스티커 에셋은 최대 {MaxAssetCount}개까지 추가할 수 있어 나머지는 건너뜁니다."));
                        break;
                    }

                    var fileName = GetDcconStickerFileName(sticker);
                    try
                    {
                        var bytes = await client.DownloadStickerAsync(sticker);
                        await AddImportedAssetAsync(fileName, bytes);
                        addedCount++;
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        failureMessages.Add($"{sticker.Title} ({stickerException.Message})");
                    }
                }

                await ReportImportResultAsync("디시콘", addedCount, failedCount, failureMessages);
            });
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"디시콘을 불러오는 중 오류가 발생했습니다: {exception.Message}")); }
    }

    // Downloads the DCCon package main image and applies it as the sticker icon; a failure is
    // reported but does not stop the asset import.
    private async Task TrySetDcconIconAsync(DcconClient client, DcconPackageDetail detail)
    {
        try
        {
            var iconBytes = await client.DownloadStickerAsync(new DcconSticker { Path = detail.MainImagePath });
            var iconExtension = detail.Stickers.Count > 0 ? detail.Stickers[0].Extension : "png";
            var iconFileName = string.IsNullOrWhiteSpace(detail.MainImagePath) ? $"sticker_icon.{iconExtension}" : $"{detail.MainImagePath}.{iconExtension}";

            await SetIconAsync(iconFileName, iconBytes);
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"아이콘을 불러오지 못했습니다: {exception.Message}")); }
    }

    private static string GetDcconStickerFileName(DcconSticker sticker) => $"{(!string.IsNullOrWhiteSpace(sticker.Path) ? sticker.Path : $"sticker_{sticker.SortNumber}")}.{sticker.Extension}";

    // Imports an Inven sticker package: the package id is parsed from the shop URL, and the
    // fetched title and tags fill only the empty form fields.
    private async Task ImportInvenStickerAsync()
    {
        var url = await ShowInputDialogAsync(new InputDialogParameters("인벤 스티커 불러오기", "인벤 스티커 URL을 입력해주세요.", "https://imart.inven.co.kr/shop/sticker/1164", showCancel: true));
        if (string.IsNullOrWhiteSpace(url)) return;

        var packageIdMatch = Regex.Match(url, @"/shop/sticker/(\d+)");
        if (!int.TryParse(packageIdMatch.Groups[1].Value, out var packageId) || packageId <= 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "올바른 인벤 스티커 URL을 입력해주세요."));
            return;
        }

        try
        {
            using var client = new InvenStickerClient();
            var detail = await client.GetDetailAsync(packageId);

            if (string.IsNullOrWhiteSpace(Name)) Name = detail.Title;
            if (string.IsNullOrWhiteSpace(Category) && detail.Tags.Count > 0) Category = string.Join(", ", detail.Tags.Select(x => x.Replace("#", string.Empty)));

            await ExecuteWithLoadingAsync(async () =>
            {
                await TrySetInvenStickerIconAsync(client, detail);

                var addedCount = 0;
                var failedCount = 0;
                List<string> failureMessages = [];
                for (var index = 0; index < detail.Images.Count; index++)
                {
                    if (Assets.Count >= MaxAssetCount)
                    {
                        await ShowMessageDialogAsync(new MessageDialogParameters("알림", $"스티커 에셋은 최대 {MaxAssetCount}개까지 추가할 수 있어 나머지는 건너뜁니다."));
                        break;
                    }

                    var fileName = InvenStickerFileNameHelper.GetStickerFileName(detail.Images[index], index);
                    try
                    {
                        var bytes = await client.DownloadImageAsync(detail.Images[index]);
                        await AddImportedAssetAsync(fileName, bytes);
                        addedCount++;
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        failureMessages.Add($"{fileName} ({stickerException.Message})");
                    }
                }

                await ReportImportResultAsync("인벤", addedCount, failedCount, failureMessages);
            });
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"인벤 스티커를 불러오는 중 오류가 발생했습니다: {exception.Message}")); }
    }

    // Downloads the Inven sticker package thumbnail and applies it as the sticker icon; a
    // failure is reported but does not stop the asset import.
    private async Task TrySetInvenStickerIconAsync(InvenStickerClient client, InvenStickerPackageDetail detail)
    {
        try
        {
            var iconBytes = await client.DownloadImageAsync(new InvenStickerImage { Url = detail.ThumbnailUrl });
            var iconExtension = detail.Images.Count > 0 ? detail.Images[0].Extension : "png";

            await SetIconAsync($"sticker_icon.{iconExtension}", iconBytes);
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"아이콘을 불러오지 못했습니다: {exception.Message}")); }
    }

    // Imports an Arca Live emoticon package: the emoticon id is parsed from the URL and the
    // sticker list is fetched from the public JSON API.
    private async Task ImportArcaLiveEmoticonAsync()
    {
        var url = await ShowInputDialogAsync(new InputDialogParameters("아카콘 불러오기", "아카콘 URL을 입력해주세요.", "https://arca.live/e/52863", showCancel: true));
        if (string.IsNullOrWhiteSpace(url)) return;

        var emoticonIndex = Regex.Match(url, @"/(?:e|emoticon)/(\d+)").Groups[1].Value;
        if (!int.TryParse(emoticonIndex, out var emoticonId) || emoticonId <= 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "올바른 아카콘 URL을 입력해주세요."));
            return;
        }

        try
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var stickers = await FetchArcaLiveEmoticonsAsync(emoticonId);
                if (stickers.Count == 0)
                {
                    await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "아카콘 스티커를 불러오지 못했습니다."));
                    return;
                }

                var addedCount = 0;
                var failedCount = 0;
                List<string> failureMessages = [];
                using var httpClient = new HttpClient();
                foreach (var stickerNode in stickers)
                {
                    if (Assets.Count >= MaxAssetCount)
                    {
                        await ShowMessageDialogAsync(new MessageDialogParameters("알림", $"스티커 에셋은 최대 {MaxAssetCount}개까지 추가할 수 있어 나머지는 건너뜁니다."));
                        break;
                    }

                    var imageUrl = stickerNode["imageUrl"]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(imageUrl)) continue;

                    if (imageUrl.StartsWith("//")) imageUrl = $"https:{imageUrl}";

                    var fileName = GetArcaLiveEmoticonFileName(imageUrl);
                    try
                    {
                        var bytes = await httpClient.GetByteArrayAsync(imageUrl);
                        var uniqueFileName = await AddImportedAssetAsync(fileName, bytes);
                        addedCount++;

                        // Arca Live packages expose no separate representative image, so the first
                        // downloaded sticker doubles as the icon until the user replaces it.
                        if (_iconBytes == null) await SetIconAsync(uniqueFileName, bytes);
                    }
                    catch (Exception stickerException)
                    {
                        failedCount++;
                        failureMessages.Add($"{fileName} ({stickerException.Message})");
                    }
                }

                await ReportImportResultAsync("아카콘", addedCount, failedCount, failureMessages);
            });
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"아카콘을 불러오는 중 오류가 발생했습니다: {exception.Message}")); }
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
        return string.IsNullOrWhiteSpace(fileName) ? $"arcalive_{Guid.NewGuid().ToString()[..8]}.png" : fileName;
    }

    // Registers downloaded image bytes as a draft asset under a unique file name and returns
    // that name, so the caller can reuse it (for example as the icon file name).
    private async Task<string> AddImportedAssetAsync(string fileName, byte[] bytes)
    {
        var uniqueFileName = BuildUniqueFileName(fileName);
        Assets.Add(new CreateStickerAssetItemViewModel(uniqueFileName, bytes, await CreateAssetPreviewAsync(bytes), this));
        return uniqueFileName;
    }

    // Applies downloaded image bytes as the sticker icon: upload name, upload bytes, and preview.
    private async Task SetIconAsync(string fileName, byte[] bytes)
    {
        _iconFileName = fileName;
        _iconBytes = bytes;
        IconSource = await CreateAssetPreviewAsync(bytes);
    }

    // Reports the import outcome: nothing imported is an error, a partial failure lists every
    // failed sticker in one dialog, and a full success confirms the imported count.
    private async Task ReportImportResultAsync(string stickerSourceName, int addedCount, int failedCount, IReadOnlyList<string> failureMessages)
    {
        if (addedCount == 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"{stickerSourceName} 스티커를 불러오지 못했습니다."));
            return;
        }

        if (failedCount > 0)
        {
            var failureList = string.Join("\n", failureMessages);
            await ShowMessageDialogAsync(new MessageDialogParameters("알림", $"스티커 {addedCount}개를 불러왔고 {failedCount}개를 불러오지 못했습니다.\n\n불러오지 못한 항목:\n{failureList}"));
            return;
        }

        await ShowMessageDialogAsync(new MessageDialogParameters("성공", $"스티커 {addedCount}개를 불러왔습니다!"));
    }

    // Validates the form and posts the create request. The success path raises Created so
    // the window closes and the sticker list refreshes.
    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_iconBytes == null)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("스티커 만들기", "아이콘을 선택해주세요."));
            return;
        }

        var name = Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("스티커 만들기", "스티커 이름을 입력해주세요."));
            return;
        }

        var category = Category?.Trim();
        if (string.IsNullOrEmpty(category))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("스티커 만들기", "카테고리를 입력해주세요."));
            return;
        }

        if (Assets.Count == 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("스티커 만들기", "스티커 에셋을 최소 1개 이상 추가해주세요."));
            return;
        }

        var assetFiles = Assets.ToDictionary(asset => asset.FileName, asset => asset.Data);
        var result = await ExecuteRequestAsync(new CreateSticker(name, category, Description?.Trim(), IsPrivate, _iconBytes, _iconFileName, assetFiles), ErrorType.BadRequest);
        if (!result.IsSuccess)
        {
            if (result.Error == ErrorType.BadRequest) await ShowMessageDialogAsync(new MessageDialogParameters("스티커 만들기", result.ErrorMessage));
            return;
        }

        HasCreated = true;
        Created?.Invoke(this, EventArgs.Empty);
    }

    private void OnAssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(AssetCountText));

    private static bool IsImageFile(string fileName) => Constants.ImageFileTypeFilters.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);

    // Duplicate file names would overwrite each other in the upload form data, so later
    // duplicates get a numeric suffix.
    private string BuildUniqueFileName(string fileName)
    {
        var candidate = fileName;
        var suffix = 2;
        while (Assets.Any(asset => string.Equals(asset.FileName, candidate, StringComparison.OrdinalIgnoreCase))) candidate = $"{Path.GetFileNameWithoutExtension(fileName)}_{suffix++}{Path.GetExtension(fileName)}";
        return candidate;
    }

    // Best-effort asset preview: formats the system image codec cannot decode (for example
    // webp without the OS codec) fall back to a PNG conversion, and a preview that still
    // fails returns null so the asset stays uploadable without a thumbnail.
    private static async Task<BitmapImage> CreateAssetPreviewAsync(byte[] imageData)
    {
        try { return await CreateBitmapImageAsync(imageData); }
        catch
        {
            try
            {
                var pngData = ImageConversionHelper.ConvertToPng(imageData);
                return pngData == null ? null : await CreateBitmapImageAsync(pngData);
            }
            catch { return null; }
        }
    }

    private static async Task<BitmapImage> CreateBitmapImageAsync(byte[] imageData)
    {
        var bitmapImage = new BitmapImage();
        using (var stream = new InMemoryRandomAccessStream())
        {
            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                using var dataWriter = new DataWriter(outputStream);
                dataWriter.WriteBytes(imageData);
                await dataWriter.StoreAsync();
                await dataWriter.FlushAsync();
            }
            stream.Seek(0);
            await bitmapImage.SetSourceAsync(stream);
        }
        return bitmapImage;
    }
}
