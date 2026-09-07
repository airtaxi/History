using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Message;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Helpers;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.IO;
using Windows.Storage.Streams;

namespace History.WindowsClient.ViewModels;

public partial class WriteMessageDialogViewModel : ObservableObject
{
    private static readonly string[] s_imageFileTypeFilters = [".png", ".apng", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff"];

    public BaseViewModel BaseViewModel { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(IsNotReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(ReceiverName))]
    [NotifyPropertyChangedFor(nameof(IsReceiverAdmin))]
    [NotifyPropertyChangedFor(nameof(IsReceiverModerator))]
    [NotifyPropertyChangedFor(nameof(ReceiverProfileImage))]
    public partial UserResponseDto SelectedReceiver { get; set; }

    public bool IsReceiverSelected => SelectedReceiver != null;
    public bool IsNotReceiverSelected => SelectedReceiver == null;

    public string ReceiverName => SelectedReceiver?.Nickname ?? SelectedReceiver?.Handle ?? SelectedReceiver?.UserId ?? string.Empty;
    public bool IsReceiverAdmin => SelectedReceiver?.Rank == Rank.Admin;
    public bool IsReceiverModerator => SelectedReceiver?.Rank == Rank.Moderator;
    public ImageSource ReceiverProfileImage => SelectedReceiver?.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(SelectedReceiver.ProfileThumbnailMediaId))) : (SelectedReceiver?.ProfileMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(SelectedReceiver.ProfileMediaId))) : null);

    [ObservableProperty]
    public partial ObservableCollection<HistoryFriendshipViewModel> SuggestedFriends { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextLengthText))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial string Text { get; set; } = string.Empty;

    public string TextLengthText => $"{Text?.Length ?? 0} / 100자";

    public bool CanSend => IsReceiverSelected && !string.IsNullOrWhiteSpace(Text) && (Text?.Length ?? 0) <= 100 && !IsSending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AttachmentVisibility))]
    public partial bool HasAttachment { get; set; }

    public bool AttachmentVisibility => HasAttachment;

    [ObservableProperty]
    public partial BitmapImage AttachmentImageSource { get; set; }

    public byte[] AttachmentData { get; private set; }
    public string AttachmentFileName { get; private set; }

    public bool IsSent { get; private set; }

    public WriteMessageDialogViewModel(BaseViewModel baseViewModel, UserResponseDto receiver = null)
    {
        BaseViewModel = baseViewModel;
        SelectedReceiver = receiver;
        PopulateDefaultSuggestions();
    }
private void PopulateDefaultSuggestions()
    {
        if (CommonShared.Friends == null)
        {
            SuggestedFriends = [];
            return;
        }

        SuggestedFriends = new(CommonShared.Friends.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed }));
    }

    public void FilterFriends(string query)
    {
        if (CommonShared.Friends == null)
        {
            SuggestedFriends.Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            PopulateDefaultSuggestions();
            return;
        }

        var filtered = CommonShared.Friends.Where(x => (x.Nickname != null && (x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))) || (x.Handle != null && x.Handle.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname);

        SuggestedFriends = new(filtered.Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed }));
    }
    [RelayCommand]
    public void SelectReceiver(UserResponseDto user) => SelectedReceiver = user;

    [RelayCommand]
    public void ClearReceiver()
    {
        SelectedReceiver = null;
        PopulateDefaultSuggestions();
    }

    [RelayCommand]
    public async Task HandleMediaTapAsync()
    {
        var result = await BaseViewModel.PickFileAsync(new FileOpenPickerParameters(s_imageFileTypeFilters, PickerLocationId.PicturesLibrary, "이미지 추가"));
        if (result == null) return;

        var fileName = Path.GetFileName(result.Path);
        var imageData = await File.ReadAllBytesAsync(result.Path);
        await ApplyAttachmentAsync(fileName, imageData);
    }

    [RelayCommand]
    public void ClearAttachment()
    {
        AttachmentData = null;
        AttachmentFileName = null;
        AttachmentImageSource = null;
        HasAttachment = false;
    }

    public async Task ApplyAttachmentAsync(string fileName, byte[] imageData)
    {
        ClearAttachment();

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

        AttachmentImageSource = bitmapImage;
        AttachmentFileName = fileName;
        AttachmentData = imageData;
        HasAttachment = true;
    }

    public async Task<bool> SendAsync()
    {
        if (SelectedReceiver == null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("오류", "받는 사람을 선택하세요."));
            return false;
        }

        var text = Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await BaseViewModel.ShowMessageDialogAsync(new("오류", "쪽지 내용을 입력하세요."));
            return false;
        }

        if (text.Length > 100)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("오류", "쪽지는 100자 이내로 작성해야 합니다."));
            return false;
        }

        IsSending = true;
        try
        {
            var contents = new List<BaseContent> { new TextContent { Text = text } };
            var files = new Dictionary<string, byte[]>();

            if (HasAttachment && AttachmentData != null && !string.IsNullOrEmpty(AttachmentFileName))
            {
                contents.Add(new UploadContent { FileName = AttachmentFileName });
                files[AttachmentFileName] = AttachmentData;
            }

            var result = await BaseViewModel.ExecuteRequestAsync(new SendMessage(SelectedReceiver.UserId, contents, files));
            if (result.IsSuccess)
            {
                IsSent = true;
                await BaseViewModel.ShowMessageDialogAsync(new("성공", "쪽지가 전송되었습니다."));
                return true;
            }

            return false;
        }
        finally { IsSending = false; }
    }
}
