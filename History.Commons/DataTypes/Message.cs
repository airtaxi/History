using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.Commons.DataTypes;

public class Message
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string ReceiverId { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}