using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.Commons.DataTypes.ResponseDtos;

public class MessageResponseDto
{
    public string Id { get; set; }
    public UserResponseDto Sender { get; set; }
    public UserResponseDto Receiver { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}