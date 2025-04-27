
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
    /// Received but not yet accepted.
    /// </summary>
    Waiting,

    /// <summary>
    /// Friend request is accepted.
    /// </summary>
    Accepted,

    /// <summary>
    /// User is ignored.
    /// </summary>
    Ignored,

    /// <summary>
    /// User is blocked.
    /// </summary>
    Blocked
}
