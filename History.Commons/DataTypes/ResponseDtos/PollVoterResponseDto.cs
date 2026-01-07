namespace History.Commons.DataTypes.ResponseDtos;

public class PollVoterResponseDto
{
    public UserResponseDto User { get; set; }
    public DateTime VotedAt { get; set; }
}
