using CommunityToolkit.Mvvm.Messaging;
using FFImageLoading.Maui;
using History.Commons.Api.Sticker;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using Microsoft.Maui.Controls.Shapes;
using UraniumUI.Icons.MaterialSymbols;
using Path = System.IO.Path;

namespace History.MobileClient.Pages;

public partial class CreateStickerPage : ContentPage
{
    private bool _isInForeground;
    private string _iconFilePath;
    private readonly List<string> _assetFilePaths = [];
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
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnIconGridTapped(object sender, TappedEventArgs e)
    {
        var result = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
        {
            Title = "아이콘 선택",
            SelectionLimit = 1
        });

        if (result == null || result.Count == 0) return;

        // Validation
        var mimeType = History.Commons.MimeTypes.GetMimeType(result[0].FileName);
        if (!mimeType.StartsWith("image/"))
        {
            await DisplayAlertAsync("오류", "이미지 파일만 선택할 수 있습니다.", "확인");
            return;
        }
        if (mimeType.Contains("gif"))
        {
            await DisplayAlertAsync("오류", "정적 이미지만 사용 가능합니다. (움짤 불가)", "확인");
            return;
        }

        _iconFilePath = result[0].FullPath;
        IconImage.Source = ImageSource.FromFile(result[0].FullPath);
        IconImage.IsVisible = true;
        IconPlaceholderImage.IsVisible = false;
    }

    private async void OnAddAssetsButtonClicked(object sender, EventArgs e)
    {
        if (_assetFilePaths.Count >= MaxAssets)
        {
            await DisplayAlertAsync("오류", $"스티커 에셋은 최대 {MaxAssets}개까지 추가할 수 있습니다.", "확인");
            return;
        }

        var remainingSlots = MaxAssets - _assetFilePaths.Count;

#if ANDROID
        var results = await AndroidMediaPickerHelper.PickMultipleImagesAsync();
#else
        var results = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "이미지 선택",
            FileTypes = FilePickerFileType.Images
        });
#endif

        if (results == null) return;

        var addedCount = 0;
        foreach (var result in results)
        {
            if (addedCount >= remainingSlots) break;

            var mimeType = History.Commons.MimeTypes.GetMimeType(result.FileName);
            if (!mimeType.StartsWith("image/") || mimeType.Contains("gif")) continue;

            _assetFilePaths.Add(result.FullPath);
            AddAssetToUI(result.FullPath);
            addedCount++;
        }

        UpdateAssetCount();
    }

    private void AddAssetToUI(string filePath)
    {
        var grid = new Grid
        {
            HeightRequest = 80,
            WidthRequest = 80,
            Margin = new Thickness(0, 0, 8, 8)
        };

        var image = new CachedImage
        {
            Source = ImageSource.FromFile(filePath),
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
            }
        };
        deleteButton.StrokeShape = new RoundRectangle { CornerRadius = 12 };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            _assetFilePaths.Remove(filePath);
            AssetsFlexLayout.Children.Remove(grid);
            UpdateAssetCount();
        };
        deleteButton.GestureRecognizers.Add(tapGesture);

        grid.Children.Add(image);
        grid.Children.Add(deleteButton);

        AssetsFlexLayout.Children.Add(grid);
    }

    private void UpdateAssetCount()
    {
        AssetCountLabel.Text = $"({_assetFilePaths.Count}/{MaxAssets})";
    }

    private async void OnCreateButtonClicked(object sender, EventArgs e)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(_iconFilePath))
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

        if (_assetFilePaths.Count == 0)
        {
            await DisplayAlertAsync("오류", "스티커 에셋을 최소 1개 이상 추가해주세요.", "확인");
            return;
        }

        CreateButton.IsEnabled = false;
        MainActivityIndicator.IsRunning = true;

        try
        {
            // Read files
            var iconBytes = await File.ReadAllBytesAsync(_iconFilePath);
            var iconFileName = Path.GetFileName(_iconFilePath);

            var assetFiles = new Dictionary<string, byte[]>();
            foreach (var assetPath in _assetFilePaths)
            {
                var assetBytes = await File.ReadAllBytesAsync(assetPath);
                var assetFileName = Path.GetFileName(assetPath);
                assetFiles[assetFileName] = assetBytes;
            }

            var result = await App.ExecuteRequestAsync(new CreateSticker(
                NameEntry.Text.Trim(),
                CategoryEntry.Text.Trim(),
                DescriptionEditor.Text?.Trim(),
                PrivateSwitch.IsToggled,
                iconBytes,
                iconFileName,
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
        finally
        {
            CreateButton.IsEnabled = true;
            MainActivityIndicator.IsRunning = false;
        }
    }
}
