using System.Text.Json;
using System.Text.Json.Serialization;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.Commons.KakaoStory;

// Source-generated metadata for the legacy Kakao Story payloads. This context keeps the
// Json.NET-compatible reading rules the existing code was written against: case-insensitive
// property names, public fields included, unknown members ignored, and numbers accepted as
// strings. Writing follows System.Text.Json defaults (null members included); the call sites
// that need null members omitted use KakaoStoryIgnoreNullJsonSerializerContext. Every call
// site obtains its JsonTypeInfo through the JsonTypeInfo overloads of KakaoStoryJsonSerializer
// below, which keeps the trimmed/AOT build free of IL2026/IL3050 warnings.
[JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ProfileData.ProfileObject))]
[JsonSerializable(typeof(HighlightData.Highlight))]
[JsonSerializable(typeof(ProfileData.Profile))]
[JsonSerializable(typeof(AuthController))]
[JsonSerializable(typeof(EmoticonItems))]
[JsonSerializable(typeof(ProfileRelationshipData.ProfileRelationship))]
[JsonSerializable(typeof(TimeLineData.TimeLine))]
[JsonSerializable(typeof(FriendData.Friends))]
[JsonSerializable(typeof(SearchData.SearchResults))]
[JsonSerializable(typeof(List<InvitationData.Invitation>))]
[JsonSerializable(typeof(List<ProfileData.Profile>))]
[JsonSerializable(typeof(BookmarkData.Bookmarks))]
[JsonSerializable(typeof(TimeLineData.Scrap))]
[JsonSerializable(typeof(List<ShareData.Share>))]
[JsonSerializable(typeof(List<CommentData.Comment>))]
[JsonSerializable(typeof(UserProfile.ProfileData))]
[JsonSerializable(typeof(List<Actor>))]
[JsonSerializable(typeof(List<CommentLikes>))]
[JsonSerializable(typeof(CommentData.Comment))]
[JsonSerializable(typeof(List<QuoteData>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(CommentData.PostData))]
[JsonSerializable(typeof(List<MailData.Mail>))]
[JsonSerializable(typeof(MailData.MailDetail))]
[JsonSerializable(typeof(NotificationStatus))]
[JsonSerializable(typeof(List<Notification>))]
[JsonSerializable(typeof(MediaData))]
[JsonSerializable(typeof(UploadedImageProp))]
[JsonSerializable(typeof(VideoData.Video))]
[JsonSerializable(typeof(VideoData.Percent))]
[JsonSerializable(typeof(SdkToken))]
[JsonSerializable(typeof(QuoteData))]
[JsonSerializable(typeof(JsonElement))]
// Several payload types share a simple name, and the source generator only emits metadata for the
// first type it detects per name (SYSLIB1031). The others would stay unresolvable at runtime, so
// each duplicate gets its own TypeInfoPropertyName; GetTypeInfo(typeof(T)) still finds them all.
[JsonSerializable(typeof(FriendData.Profile), TypeInfoPropertyName = "FriendDataProfile")]
[JsonSerializable(typeof(List<FriendData.Profile>), TypeInfoPropertyName = "ListFriendDataProfile")]
[JsonSerializable(typeof(VideoData.Info), TypeInfoPropertyName = "VideoDataInfo")]
[JsonSerializable(typeof(UserProfile.StatusObject), TypeInfoPropertyName = "UserProfileStatusObject")]
[JsonSerializable(typeof(List<UserProfile.StatusObject>), TypeInfoPropertyName = "ListUserProfileStatusObject")]
[JsonSerializable(typeof(InvitationData.StatusObject), TypeInfoPropertyName = "InvitationDataStatusObject")]
[JsonSerializable(typeof(List<InvitationData.StatusObject>), TypeInfoPropertyName = "ListInvitationDataStatusObject")]
[JsonSerializable(typeof(UploadedImageProp.Medium), TypeInfoPropertyName = "UploadedImagePropMedium")]
[JsonSerializable(typeof(TimeLineData.Relation), TypeInfoPropertyName = "TimeLineDataRelation")]
[JsonSerializable(typeof(ShareData.Actor), TypeInfoPropertyName = "ShareDataActor")]
[JsonSerializable(typeof(SearchData.Relation), TypeInfoPropertyName = "SearchDataRelation")]
[JsonSerializable(typeof(ProfileRelationshipData.Relation2), TypeInfoPropertyName = "ProfileRelationshipDataRelation2")]
[JsonSerializable(typeof(MailData.Relation), TypeInfoPropertyName = "MailDataRelation")]
[JsonSerializable(typeof(CommentLikes.Actor), TypeInfoPropertyName = "CommentLikesActor")]
[JsonSerializable(typeof(CommentData.Actor), TypeInfoPropertyName = "CommentDataActor")]
[JsonSerializable(typeof(CommentData.Relation), TypeInfoPropertyName = "CommentDataRelation")]
[JsonSerializable(typeof(Relation), TypeInfoPropertyName = "DataTypeRelation")]
[JsonSerializable(typeof(FriendData.Relation), TypeInfoPropertyName = "FriendDataRelation")]
[JsonSerializable(typeof(FriendData.StatusObject), TypeInfoPropertyName = "FriendDataStatusObject")]
[JsonSerializable(typeof(List<FriendData.StatusObject>), TypeInfoPropertyName = "ListFriendDataStatusObject")]
[JsonSerializable(typeof(ShareData.Relation), TypeInfoPropertyName = "ShareDataRelation")]
[JsonSerializable(typeof(CommentLikes.Relation), TypeInfoPropertyName = "CommentLikesRelation")]
[JsonSerializable(typeof(CommentData.StatusObject), TypeInfoPropertyName = "CommentDataStatusObject")]
[JsonSerializable(typeof(List<CommentData.StatusObject>), TypeInfoPropertyName = "ListCommentDataStatusObject")]
internal partial class KakaoStoryJsonSerializerContext : JsonSerializerContext;
