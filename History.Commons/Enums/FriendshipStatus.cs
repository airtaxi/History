
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<FriendshipStatus>))]
public enum FriendshipStatus
{
    /// <summary>
    /// Friend request is sent.
    /// </summary>
    Requested,

    /// <summary>
    /// Friend request is declined.
    /// </summary>
    Declined,

    /// <summary>
    /// Friend request is accepted.
    /// </summary>
    Accepted,

    /// <summary>
    /// User is blocked.
    /// </summary>
    Blocked,

    /// <summary>
    /// User is ignored.
    /// </summary>
    Ignored
}
