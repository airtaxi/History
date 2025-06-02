using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<RestrictionType>))]
public enum RestrictionType
{
    PostDeletion,
    CommentDeletion
}
