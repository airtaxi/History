using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient;
using History.MobileClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class MessageViewModel : ObservableObject
{
    [ObservableProperty]
    private MessageResponseDto _message;

    public string Id => _message.Id;
    public UserResponseDto Sender => _message.Sender;
    public UserResponseDto Receiver => _message.Receiver;
    public List<BaseContent> Contents => _message.Contents;
    public DateTime CreatedAt => _message.CreatedAt;
    public string MainText => Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;
    public string ImageUrl => Contents.OfType<MediaContent>().FirstOrDefault()?.MediaId != null ? Utils.GenerateMediaUri(Contents.OfType<MediaContent>().First().MediaId) : null;
    public bool HasImage => !string.IsNullOrEmpty(ImageUrl);
    public string SenderName => Sender?.Nickname ?? Sender?.Handle ?? Sender?.UserId;
    public string ReceiverName => Receiver?.Nickname ?? Receiver?.Handle ?? Receiver?.UserId;
    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, _message.ModifiedAt);

    public MessageViewModel(MessageResponseDto message)
    {
        _message = message;
    }

    [RelayCommand]
    public async Task OpenMessage()
    {
        await App.PushAsync(new MessagePage(this));
    }
}
