using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<NotificationType>))]
public enum NotificationType
{
    Comment,
    CommentMention,
    CommentLike,
    Share,
    Repost,
    PostReaction,
    PostMention,
    FriendRequest,
    FavoriteFriendNewPost,
    Birthday,
    Restriction,
    Report
}
