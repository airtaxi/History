namespace History.Commons.DataTypes.RequestDtos;

public class VotePollRequestDto
{
    /// <summary>
    /// Selected option indices.
    /// </summary>
    public List<int> SelectedOptionIndices { get; set; } = [];
}
