using System.Text.Json;
using System.Text.Json.Serialization;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;

namespace History.Commons.Serialization;

// Source-generated metadata for the JSON API transport. The options mirror the default
// System.Text.Json web serializer the previous transport used (JsonSerializerDefaults.Web)
// for both AddJsonBody-style request bodies and response deserialization: camelCase property
// names, case-insensitive reads, and numbers accepted as strings.
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(CommentResponseDto))]
[JsonSerializable(typeof(ExternalUrlContent))]
[JsonSerializable(typeof(GetUserPostsCountResponseDto))]
[JsonSerializable(typeof(InviteCodeRequestResponseDto))]
[JsonSerializable(typeof(MessageResponseDto))]
[JsonSerializable(typeof(OAuthLoginResponseDto))]
[JsonSerializable(typeof(PostResponseDto))]
[JsonSerializable(typeof(StickerResponseDto))]
[JsonSerializable(typeof(UserResponseDto))]
[JsonSerializable(typeof(List<CommentResponseDto>))]
[JsonSerializable(typeof(List<InviteCodeRequestResponseDto>))]
[JsonSerializable(typeof(List<InviteCodeResponseDto>))]
[JsonSerializable(typeof(List<MessageResponseDto>))]
[JsonSerializable(typeof(List<ModerationRecordResponseDto>))]
[JsonSerializable(typeof(List<NotificationResponseDto>))]
[JsonSerializable(typeof(List<PollVoterResponseDto>))]
[JsonSerializable(typeof(List<PostResponseDto>))]
[JsonSerializable(typeof(List<StickerAssetResponseDto>))]
[JsonSerializable(typeof(List<StickerResponseDto>))]
[JsonSerializable(typeof(List<UserResponseDto>))]
[JsonSerializable(typeof(BulkChangeDiscoveryOptionByPostIdsRequestDto))]
[JsonSerializable(typeof(BulkChangeDiscoveryOptionRequestDto))]
[JsonSerializable(typeof(BulkDeletePostsByPostIdsRequestDto))]
[JsonSerializable(typeof(ChangeDiscoveryOptionRequestDto))]
[JsonSerializable(typeof(CreateInviteCodeByAdminRequestDto))]
[JsonSerializable(typeof(CreateInviteCodeRequestDto))]
[JsonSerializable(typeof(CreateReportRecordRequestDto))]
[JsonSerializable(typeof(LogoutRequestDto))]
[JsonSerializable(typeof(OAuthLoginRequestDto))]
[JsonSerializable(typeof(OAuthRegisterRequestDto))]
[JsonSerializable(typeof(ProcessInviteCodeRequestDto))]
[JsonSerializable(typeof(ReadNotificationsRequestDto))]
[JsonSerializable(typeof(RefreshTokenRequestDto))]
[JsonSerializable(typeof(UpdateKakaoStoryTokenRequestDto))]
[JsonSerializable(typeof(UpdateMemoRequestDto))]
[JsonSerializable(typeof(UpdateMessageReceivingPermissionRequestDto))]
[JsonSerializable(typeof(UpdatePushNotificationPermissionRequestDto))]
[JsonSerializable(typeof(UpdateUserBirthdayRequestDto))]
[JsonSerializable(typeof(UpdateUserDescriptionRequestDto))]
[JsonSerializable(typeof(UpdateUserHandleRequestDto))]
[JsonSerializable(typeof(UpdateUserNicknameRequestDto))]
[JsonSerializable(typeof(VotePollRequestDto))]
public partial class ApiWebJsonSerializerContext : JsonSerializerContext;
