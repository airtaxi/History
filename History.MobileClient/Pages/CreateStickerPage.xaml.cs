using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using FFImageLoading.Maui;
using History.Commons.Api.Sticker;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using Microsoft.Maui.Controls.Shapes;
using UraniumUI.Icons.MaterialSymbols;

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

        if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) _ = Toast.Make("WebP 움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();

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
            if (!mimeType.StartsWith("image/") || mimeType.Contains("gif")) continue;

            if (image.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) && addedCount == 0)
                _ = Toast.Make("WebP 움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();

            var memoryStream = new MemoryStream(image.Bytes);
            _assets.Add(image.FileName, memoryStream);
            AddAssetToUI(image.FileName, memoryStream);
            addedCount++;
        }
#else
        var request = new MediaPickRequest(maxCount, MediaFileType.Image) { Title = "이미지 추가" };

        var results = await MediaGallery.PickAsync(request);
        var files = results?.Files?.ToArray();
        if (files == null || files.Length == 0) return;

        if (files.Any(x => x.Extension.Equals("webp", StringComparison.OrdinalIgnoreCase))) _ = Toast.Make("WebP 움짤 파일의 경우 업로드 처리에 시간이 오래 걸릴 수 있습니다.").Show();
            
        foreach (var file in files)
        {
            using var stream = await file.OpenReadAsync();
            var memoryStream = new MemoryStream(); // Keep the stream open (do not use 'using' here)
            await stream.CopyToAsync(memoryStream); 
            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileName = file.GenerateFileName();
            var mimeType = Commons.MimeTypes.GetMimeType(fileName);
            if (!mimeType.StartsWith("image/") || mimeType.Contains("gif")) continue;

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

    private void UpdateAssetCount()
    {
        AssetCountLabel.Text = $"({_assets.Count}/{MaxAssets})";
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
