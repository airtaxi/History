namespace History.Commons.DataTypes.RequestDtos;

public class GetUserPostsCountResponseDto()
{
    public long Count { get; }

    public GetUserPostsCountResponseDto(long count) : this() => Count = count;
}