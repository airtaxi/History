using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace History.Commons.DataTypes.Contents;

[BsonKnownTypes(typeof(ProfileContent), typeof(MediaContent), typeof(UploadContent), typeof(TextContent), typeof(StickerContent))]
[JsonDerivedType(typeof(ProfileContent), "profile")]
[JsonDerivedType(typeof(MediaContent), "media")]
[JsonDerivedType(typeof(UploadContent), "upload")]
[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(StickerContent), "sticker")]
public class BaseContent;
