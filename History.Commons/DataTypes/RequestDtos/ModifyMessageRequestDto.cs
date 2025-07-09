using History.Commons.DataTypes.Contents;

namespace History.Commons.DataTypes.RequestDtos;

public class ModifyMessageRequestDto
{
    public List<BaseContent> Contents { get; set; } = [];
}