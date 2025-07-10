using History.Commons.DataTypes.Contents;

namespace History.Commons.DataTypes.RequestDtos;

public class SendMessageRequestDto
{
    public string ReceiverId { get; set; } = null!;
    public List<BaseContent> Contents { get; set; } = [];
}