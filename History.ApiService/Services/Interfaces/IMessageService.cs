using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.AspNetCore.Http;

namespace History.ApiService.Services.Interfaces;

public interface IMessageService
{
    /// <summary>
    /// Get message by id.
    /// </summary>
    /// <param name="messageId">The id of message to get</param>
    /// <returns>A task that represents the asynchronous operation with result of message</returns>
    Task<Result<Message>> GetMessageByIdAsync(string messageId);

    /// <summary>
    /// Get received messages for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="fromMessageId">The message ID to start from for pagination</param>
    /// <param name="limit">The maximum number of messages to return</param>
    /// <returns>A task that represents the asynchronous operation with result of messages</returns>
    Task<Result<List<Message>>> GetReceivedMessagesAsync(string userId, string fromMessageId = null, int limit = 50);

    /// <summary>
    /// Get sent messages for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="fromMessageId">The message ID to start from for pagination</param>
    /// <param name="limit">The maximum number of messages to return</param>
    /// <returns>A task that represents the asynchronous operation with result of messages</returns>
    Task<Result<List<Message>>> GetSentMessagesAsync(string userId, string fromMessageId = null, int limit = 50);

    /// <summary>
    /// Send a message to another user.
    /// </summary>
    /// <param name="senderId">The sender user ID</param>
    /// <param name="requestDto">The message data</param>
    /// <param name="files">The files to upload</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<Result> SendMessageAsync(string senderId, SendMessageRequestDto requestDto, IEnumerable<IFormFile> files);

    /// <summary>
    /// Mark a message as read.
    /// </summary>
    /// <param name="messageId">The message ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<Result> MarkMessageAsReadAsync(string messageId, string userId);

    /// <summary>
    /// Check if user can send message to another user.
    /// </summary>
    /// <param name="senderId">The sender user ID</param>
    /// <param name="receiverId">The receiver user ID</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<Result> CheckMessagePermissionAsync(string senderId, string receiverId);

    /// <summary>
    /// Generate message response DTOs from messages.
    /// </summary>
    /// <param name="messages">The messages</param>
    /// <param name="requesterId">The requester user ID</param>
    /// <returns>A task that represents the asynchronous operation with result of message DTOs</returns>
    Task<Result<List<MessageResponseDto>>> GenerateMessageResponseDtosAsync(List<Message> messages, string requesterId);

    /// <summary>
    /// Generate a single message response DTO from a message.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="requesterId">Th requester user ID</param>
    /// <returns>A task that represents the asynchronous operation with result of message DTO</returns>
    Task<Result<MessageResponseDto>> GenerateMessageResponseDtoAsync(Message message, string requesterId);

    /// <summary>
    /// Handle user withdrawal by cleaning up message data.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<Result> HandleWithdrawAsync(string userId);
}