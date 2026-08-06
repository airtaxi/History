using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.DataTypes;

public class CommentTappedMessage : ValueChangedMessage<CommentTarget>
{
    public CommentTappedMessage(UserResponseDto value) : base(new CommentTarget(value.UserId, value.Nickname)) { }

    public CommentTappedMessage(string userId, string nickname) : base(new CommentTarget(userId, nickname)) { }
}
