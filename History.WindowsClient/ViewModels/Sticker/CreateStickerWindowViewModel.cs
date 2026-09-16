using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Sticker;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels.Sticker;

// Sticker create window state: icon/asset picking, form validation, and the create request
// that closes the window with a success flag. External sticker imports (DCCon/Arca/Inven)
// are not wired yet: the action list opens, and picking an entry answers with a notice.
public sealed partial class CreateStickerWindowViewModel : BaseViewModel
{
    private const int MaxAssetCount = 200;
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const string UnderImplementationTitle = "안내";
    private const string UnderImplementationMessage = "구현 중인 기능입니다.";

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

    // Shows the external sticker import action list; the loaders are not wired yet.
    [RelayCommand]
    private async Task ImportExternalAsync()
    {
        var action = await ShowSelectionDialogAsync("스티커 불러오기", ExternalImportOptions);
        if (action == null) return;

        // TODO: Route each import entry to its loader once the external clients are wired.
        await ShowMessageDialogAsync(new MessageDialogParameters(UnderImplementationTitle, UnderImplementationMessage));
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
