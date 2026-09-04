using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.Messages;

public class CommentReplyRequestedMessage(ProfileContent value) : ValueChangedMessage<ProfileContent>(value);