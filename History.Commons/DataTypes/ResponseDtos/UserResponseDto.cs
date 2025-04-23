using History.Commons.Enums;

namespace History.Commons.DataTypes.ResponseDtos;

public class UserResponseDto()
{
    public string UserId { get; set; }

    public Rank Rank { get; set; }
    public SocialService SocialService { get; set; }

    public DiscoveryOption LastUsedPostDiscoveryOption { get; set; }

    public string Nickname { get; set; }
    public DateTime? Birthday { get; set; }
    public string Description { get; set; }

    public string ProfileMediaId { get; set; }
    public bool UsesAnimatedProfileMedia { get; set; }

    public string BackgroundMediaId { get; set; }
    public bool UsesAnimatedBackgroundMedia { get; set; }


    public Friendship Friendship { get; set; }

    public UserResponseDto(User user) : this()
    {
        UserId = user.Id;

        Rank = user.Rank;
        SocialService = user.SocialService;

        LastUsedPostDiscoveryOption = user.LastUsedPostDiscoveryOption;

        Nickname = user.Nickname;
        Birthday = user.Birthday;
        Description = user.Description;

        ProfileMediaId = user.ProfileMediaId;
        UsesAnimatedProfileMedia = user.UsesAnimatedProfileMedia;

        BackgroundMediaId = user.BackgroundMediaId;
        UsesAnimatedBackgroundMedia = user.UsesAnimatedBackgroundMedia;
    }
}
