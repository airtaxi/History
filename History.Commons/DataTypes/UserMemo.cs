using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Attributes;

namespace History.Commons.DataTypes;

public class UserMemo
{
    [BsonId]
    public string Id { get; set; }

    public string UserId { get; set; }
    public string RegisteredBy { get; set; }

    public string Memo { get; set; }
}
