using System.Collections.Generic;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class MessageService(IMongoDatabase database, IMediaService mediaService, INotificationService notificationService, IServiceProvider serviceProvider) : IMessageService
{
    private readonly IMongoCollection<Message> _messageCollection = database.GetCollection<Message>("Messages");

    /// <inheritdoc />
    public async Task<Result<Message>> GetMessageByIdAsync(string messageId)
    {
        var message = await _messageCollection.Find(m => m.Id == messageId).FirstOrDefaultAsync();
        if (message == null) return (ErrorType.NotFound, "쪽지를 찾을 수 없습니다.");
        return message;
    }

    /// <inheritdoc />
    public async Task<Result<List<Message>>> GetReceivedMessagesAsync(string userId, string fromMessageId = null, int limit = 50)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.ReceiverId, userId);
        if (!string.IsNullOrEmpty(fromMessageId))
        {
            var fromMessage = await _messageCollection.Find(m => m.Id == fromMessageId).FirstOrDefaultAsync();
            if (fromMessage != null)
            {
                filter &= Builders<Message>.Filter.Lt(m => m.CreatedAt, fromMessage.CreatedAt);
            }
        }
        var messages = await _messageCollection
            .Find(filter)
            .Sort(Builders<Message>.Sort.Descending(m => m.CreatedAt))
            .Limit(limit)
            .ToListAsync();
        return messages;
    }

    /// <inheritdoc />
    public async Task<Result<List<Message>>> GetSentMessagesAsync(string userId, string fromMessageId = null, int limit = 50)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.SenderId, userId);
        if (!string.IsNullOrEmpty(fromMessageId))
        {
            var fromMessage = await _messageCollection.Find(m => m.Id == fromMessageId).FirstOrDefaultAsync();
            if (fromMessage != null)
            {
                filter &= Builders<Message>.Filter.Lt(m => m.CreatedAt, fromMessage.CreatedAt);
            }
        }
        var messages = await _messageCollection
            .Find(filter)
            .Sort(Builders<Message>.Sort.Descending(m => m.CreatedAt))
            .Limit(limit)
            .ToListAsync();
        return messages;
    }

    /// <inheritdoc />
    public async Task<Result> SendMessageAsync(string senderId, SendMessageRequestDto requestDto, IEnumerable<IFormFile> files)
    {
        var checkResult = await CheckMessagePermissionAsync(senderId, requestDto.ReceiverId);
        if (checkResult.IsFailure) return checkResult;

        var userService = serviceProvider.GetRequiredService<IUserService>();
        var receiverResult = await userService.GetUserByIdAsync(requestDto.ReceiverId);
        if (receiverResult.IsFailure) return receiverResult.CastFailure();

        // Sanitize contents
        var contents = requestDto.Contents ?? [];
        Utils.SanitizeContents(contents);

        // Check if user is equal to receiver
        if (senderId == requestDto.ReceiverId)
            return (ErrorType.BadRequest, "자기 자신에게 쪽지를 보낼 수 없습니다.");

        // Check for external URLs
        if (contents.Any(c => c is ExternalUrlContent))
            return (ErrorType.BadRequest, "쪽지에는 외부 URL을 첨부할 수 없습니다.");

        // Check if the message has any content
        if (contents.Count == 0 || (contents.Count == 1 && contents.First() is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text)))
            return (ErrorType.BadRequest, "쪽지에 내용이 없습니다.");

        // Check text length
        var textContents = contents.OfType<TextContent>();
        var text = string.Join("", textContents.Select(tc => tc.Text));
        text = Utils.SanitizeText(text);
        if (text.Length > 100)
            return (ErrorType.BadRequest, "쪽지는 100자 이내로 작성해야 합니다.");

        // Validate media contents (limit to 1 image)
        var mediaCount = contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 1) return (ErrorType.BadRequest, "쪽지에는 이미지를 최대 1개까지만 첨부할 수 있습니다.");

        var mediaContents = contents.OfType<MediaContent>();
        foreach (var mediaContent in mediaContents)
        {
            if (string.IsNullOrEmpty(mediaContent.MediaId) || string.IsNullOrEmpty(mediaContent.MimeType) || mediaContent.ThumbnailMediaId == null)
            {
                return (ErrorType.BadRequest, "미디어 콘텐츠는 MediaId, MimeType, ThumbnailMediaId가 모두 필요합니다.");
            }
        }


        var finalTextContent = new TextContent() { Text = text };
        var finalMediaConent = mediaContents.FirstOrDefault();
        var finalContents = new List<BaseContent>() { finalTextContent };
        if (finalMediaConent != null) finalContents.Add(finalMediaConent);

        string messageId;
        while (true)
        {
            messageId = Guid.NewGuid().ToString("N");
            var existingMessage = await GetMessageByIdAsync(messageId);
            if (existingMessage.IsFailure) break;
        }

        // Upload media
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Message, messageId, senderId, contents, files);
        if (uploadResult.IsFailure) return uploadResult;

        var message = new Message
        {
            Id = messageId,
            SenderId = senderId,
            ReceiverId = requestDto.ReceiverId,
            Contents = finalContents,
            CreatedAt = DateTime.UtcNow
        };

        await _messageCollection.InsertOneAsync(message);

        // Send notification
        await notificationService.SendNotificationsAsync(NotificationType.Message, message.Id);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> MarkMessageAsReadAsync(string messageId, string userId)
    {
        var messageResult = await GetMessageByIdAsync(messageId);
        if (messageResult.IsFailure) return messageResult.CastFailure();

        var message = messageResult.Value;
        if (message.ReceiverId != userId)
        {
            return (ErrorType.Forbidden, "이 쪽지를 읽음 처리할 권한이 없습니다.");
        }

        if (message.ReadAt != null)
        {
            return Result.Success(); // Already read
        }

        var updateDef = Builders<Message>.Update.Set(m => m.ReadAt, DateTime.UtcNow);
        await _messageCollection.UpdateOneAsync(m => m.Id == messageId, updateDef);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CheckMessagePermissionAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId)
        {
            return (ErrorType.BadRequest, "자기 자신에게 쪽지를 보낼 수 없습니다.");
        }

        var userService = serviceProvider.GetRequiredService<IUserService>();
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Check blocked/blocking/ignored relationships only
        var bannedResult = await friendshipService.GetBannedUserIdsAsync(senderId);
        if (bannedResult.IsFailure) return bannedResult.CastFailure();
        if (bannedResult.Value.Contains(receiverId))
        {
            return (ErrorType.Forbidden, "상대방과의 관계로 인해 쪽지를 보낼 수 없습니다.");
        }

        // Check receiver existence and AccessPermission
        var receiverResult = await userService.GetUserByIdAsync(receiverId);
        if (receiverResult.IsFailure) return receiverResult.CastFailure();
        var receiver = receiverResult.Value;

        var messagePermission = receiver.MessageReceivingPermission;
        if (messagePermission == AccessPermission.OnlyMe)
        {
            return (ErrorType.Forbidden, "이 사용자는 쪽지 수신을 허용하지 않습니다.");
        }
        if (messagePermission == AccessPermission.Friends)
        {
            var areFriendsResult = await friendshipService.AreFriendsAsync(senderId, receiverId);
            if (areFriendsResult.IsFailure) return areFriendsResult.CastFailure();
            if (!areFriendsResult.Value)
            {
                return (ErrorType.Forbidden, "이 사용자는 친구에게서만 쪽지를 받습니다.");
            }
        }
        else if (messagePermission == AccessPermission.FriendsOfFriends)
        {
            var areFriendsOfFriendsResult = await friendshipService.AreFriendsOfFriendsAsync(senderId, receiverId);
            if (areFriendsOfFriendsResult.IsFailure) return areFriendsOfFriendsResult.CastFailure();
            if (!areFriendsOfFriendsResult.Value)
            {
                return (ErrorType.Forbidden, "이 사용자는 친구의 친구까지만 쪽지를 받습니다.");
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<MessageResponseDto>> GenerateMessageResponseDtoAsync(Message message, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var stickerService = serviceProvider.GetRequiredService<IStickerService>();

        var senderResult = await userService.GenerateUserResponseDtoAsync(message.SenderId);
        if (senderResult.IsFailure) return senderResult.CastFailure<MessageResponseDto>();

        var receiverResult = await userService.GenerateUserResponseDtoAsync(message.ReceiverId);
        if (receiverResult.IsFailure) return receiverResult.CastFailure<MessageResponseDto>();

        var sender = senderResult.Value;
        var receiver = receiverResult.Value;

        // Check if the requester is either the sender or receiver
        if (sender.UserId != requesterId && receiver.UserId != requesterId) return (ErrorType.Forbidden, "이 쪽지를 조회할 권한이 없습니다.");

        // Fill StickerMediaId for StickerContents
        var stickerContents = message.Contents.OfType<StickerContent>();
        foreach (var stickerContent in stickerContents)
        {
            var assetResult = await stickerService.GetStickerAssetByIdAsync(stickerContent.StickerContentId);
            if (assetResult.IsSuccess)
            {
                stickerContent.StickerMediaId = assetResult.Value.MediaId;
            }
        }

        var result = new MessageResponseDto
        {
            Id = message.Id,
            Sender = sender,
            Receiver = receiver,
            Contents = message.Contents,
            CreatedAt = message.CreatedAt,
            ReadAt = message.ReadAt
        };

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<List<MessageResponseDto>>> GenerateMessageResponseDtosAsync(List<Message> messages, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var stickerService = serviceProvider.GetRequiredService<IStickerService>();

        var allUserIds = messages.SelectMany(m => new[] { m.SenderId, m.ReceiverId }).Distinct().ToList();
        var usersResult = await userService.GenerateUserResponseDtosAsync(allUserIds, requesterId);
        if (usersResult.IsFailure) return usersResult.CastFailure<List<MessageResponseDto>>();

        var users = usersResult.Value;
        var results = new List<MessageResponseDto>();

        foreach (var message in messages)
        {
            var sender = users.FirstOrDefault(u => u.UserId == message.SenderId);
            var receiver = users.FirstOrDefault(u => u.UserId == message.ReceiverId);

            if (sender == null || receiver == null) continue;

            // Fill StickerMediaId for StickerContents
            var stickerContents = message.Contents.OfType<StickerContent>();
            foreach (var stickerContent in stickerContents)
            {
                var assetResult = await stickerService.GetStickerAssetByIdAsync(stickerContent.StickerContentId);
                if (assetResult.IsSuccess)
                {
                    stickerContent.StickerMediaId = assetResult.Value.MediaId;
                }
            }

            var result = new MessageResponseDto
            {
                Id = message.Id,
                Sender = sender,
                Receiver = receiver,
                Contents = message.Contents,
                CreatedAt = message.CreatedAt,
                ReadAt = message.ReadAt
            };

            results.Add(result);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return (ErrorType.BadRequest, "유저 ID가 제공되지 않았습니다.");

        // Delete all messages sent by the user
        await _messageCollection.DeleteManyAsync(m => m.SenderId == userId);

        // Delete all messages received by the user
        await _messageCollection.DeleteManyAsync(m => m.ReceiverId == userId);

        // Delete media files associated with messages
        await mediaService.DeleteMediasByUserIdAsync(userId);

        return Result.Success();
    }
}