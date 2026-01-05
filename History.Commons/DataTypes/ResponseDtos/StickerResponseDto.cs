namespace History.Commons.DataTypes.ResponseDtos;

public class StickerResponseDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string IconMediaId { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public UserResponseDto Author { get; set; }

    /// <summary>
    /// 요청자가 이 스티커를 구독했는지 여부
    /// </summary>
    public bool IsSubscribed { get; set; }

    /// <summary>
    /// 요청자가 이 스티커의 작성자인지 여부
    /// </summary>
    public bool IsOwner { get; set; }
}
