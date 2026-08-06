using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public partial class TimelineContentsViewModel : ObservableObject
{
    // Best-effort text slots (up to 3)
    [ObservableProperty] public partial TextTypeContentsViewModel Text1 { get; private set; }
    [ObservableProperty] public partial TextTypeContentsViewModel Text2 { get; private set; }
    [ObservableProperty] public partial TextTypeContentsViewModel Text3 { get; private set; }

    // Best-effort sticker slots (up to 3)
    [ObservableProperty] public partial StickerContentViewModel Sticker1 { get; private set; }
    [ObservableProperty] public partial StickerContentViewModel Sticker2 { get; private set; }
    [ObservableProperty] public partial StickerContentViewModel Sticker3 { get; private set; }

    // Single-instance slots
    [ObservableProperty] public partial BaseWrappedMediaContentsViewModel MediaContent { get; private set; }
    [ObservableProperty] public partial PollContentViewModel PollContent { get; private set; }
    [ObservableProperty] public partial ExternalUrlContentViewModel ExternalUrlContent { get; private set; }

    // Visibility flags
    [ObservableProperty] public partial bool HasText1 { get; private set; }
    [ObservableProperty] public partial bool HasText2 { get; private set; }
    [ObservableProperty] public partial bool HasText3 { get; private set; }
    [ObservableProperty] public partial bool HasSticker1 { get; private set; }
    [ObservableProperty] public partial bool HasSticker2 { get; private set; }
    [ObservableProperty] public partial bool HasSticker3 { get; private set; }
    [ObservableProperty] public partial bool HasMediaContent { get; private set; }
    [ObservableProperty] public partial bool HasPollContent { get; private set; }
    [ObservableProperty] public partial bool HasExternalUrlContent { get; private set; }

    public TimelineContentsViewModel(List<IContentViewModel> contents) => Update(contents);

    public void Update(List<IContentViewModel> contents)
    {
        var textIndex = 0;
        var stickerIndex = 0;

        // Reset all slots first to ensure stale data is cleared when UpdatePost re-runs.
        Text1 = Text2 = Text3 = null;
        Sticker1 = Sticker2 = Sticker3 = null;
        MediaContent = null;
        PollContent = null;
        ExternalUrlContent = null;

        HasText1 = HasText2 = HasText3 = false;
        HasSticker1 = HasSticker2 = HasSticker3 = false;
        HasMediaContent = HasPollContent = HasExternalUrlContent = false;

        foreach (var content in contents)
        {
            switch (content)
            {
                case TextTypeContentsViewModel text:
                    switch (textIndex)
                    {
                        case 0: Text1 = text; HasText1 = true; break;
                        case 1: Text2 = text; HasText2 = true; break;
                        case 2: Text3 = text; HasText3 = true; break;
                    }
                    textIndex++;
                    break;
                case StickerContentViewModel sticker:
                    switch (stickerIndex)
                    {
                        case 0: Sticker1 = sticker; HasSticker1 = true; break;
                        case 1: Sticker2 = sticker; HasSticker2 = true; break;
                        case 2: Sticker3 = sticker; HasSticker3 = true; break;
                    }
                    stickerIndex++;
                    break;
                case BaseWrappedMediaContentsViewModel media:
                    MediaContent = media; HasMediaContent = true;
                    break;
                case PollContentViewModel poll:
                    PollContent = poll; HasPollContent = true;
                    break;
                case ExternalUrlContentViewModel externalUrl:
                    ExternalUrlContent = externalUrl; HasExternalUrlContent = true;
                    break;
            }
        }
    }
}