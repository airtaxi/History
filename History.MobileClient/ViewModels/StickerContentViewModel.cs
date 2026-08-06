using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFImageLoading;
using FFImageLoading.Config;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using Configuration = FFImageLoading.Config.Configuration;

namespace History.MobileClient.ViewModels;

public partial class StickerContentViewModel : ObservableObject, IContentViewModel
{
    private static readonly IConfiguration s_kakaoEmoticonConfiguration = CreateKakaoEmoticonConfiguration();

    private readonly StickerContent _stickerContent;

    public StickerContentViewModel(StickerContent stickerContent)
    {
        _stickerContent = stickerContent;
        Media = new ImageViewModel(Utils.GenerateMediaUri(stickerContent.StickerMediaId))
        {
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            Aspect = Aspect.AspectFit,
            IsAnimated = stickerContent.IsAnimated
        };
    }

    // Kakao Story emoticon overload: renders the emoticon image with the
    // Referer-overridden configuration. Falls back to the placeholder text when
    // the signed URL is not available (credential not warmed up yet).
    public StickerContentViewModel(string emoticonUrl, PostType postType)
    {
        IsKakaoEmoticon = true;
        if (emoticonUrl != null)
        {
            Media = new ImageViewModel(emoticonUrl, postType)
            {
                HorizontalContentOptions = LayoutOptions.Fill,
                VerticalContentOptions = LayoutOptions.Fill,
                Aspect = Aspect.AspectFit,
                Configuration = s_kakaoEmoticonConfiguration
            };
        }
    }

    // Clones the global configuration and injects the Referer required by
    // mk.kakaocdn.net emoticon endpoints, without mutating the global config.
    private static Configuration CreateKakaoEmoticonConfiguration()
    {
        var configuration = ((Configuration)ImageService.Instance.Configuration).Clone();
        configuration.HttpHeaders = new Dictionary<string, string>(configuration.HttpHeaders ?? new Dictionary<string, string>())
        {
            ["Referer"] = "https://story.kakao.com/"
        };
        return configuration;
    }

    public string StickerId => _stickerContent?.StickerId;
    public string StickerContentId => _stickerContent?.StickerContentId;
    public bool IsKakaoEmoticon { get; }
    public ImageViewModel Media { get; }

    [RelayCommand]
    private async Task NavigateToStickerDetailAsync()
    {
        if (IsKakaoEmoticon)
        {
            await App.Page.DisplayAlertAsync("안내", "카카오스토리 이모티콘 상세 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
            return;
        }

        if (string.IsNullOrEmpty(StickerId)) return;

        var result = await App.ExecuteRequestAsync(new Commons.Api.Sticker.GetSticker(StickerId));
        if (result.IsSuccess)
        {
            await App.PushAsync(new StickerDetailPage(result.Value));
        }
    }
}
