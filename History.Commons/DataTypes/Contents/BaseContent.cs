using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace History.Commons.DataTypes.Contents;

[BsonKnownTypes(typeof(ProfileContent), typeof(ExternalUrlContent), typeof(MediaContent), typeof(UploadContent), typeof(TextContent), typeof(StickerContent), typeof(PollContent), typeof(HashtagContent), typeof(HyperlinkContent))]
[JsonDerivedType(typeof(ProfileContent), "profile")]
[JsonDerivedType(typeof(ExternalUrlContent), "externalUrl")]
[JsonDerivedType(typeof(MediaContent), "media")]
[JsonDerivedType(typeof(UploadContent), "upload")]
[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(StickerContent), "sticker")]
[JsonDerivedType(typeof(PollContent), "poll")]
[JsonDerivedType(typeof(HashtagContent), "hashtag")]
[JsonDerivedType(typeof(HyperlinkContent), "hyperlink")]
public class BaseContent;
