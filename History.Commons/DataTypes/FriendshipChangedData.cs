using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.Commons.DataTypes;

public record FriendshipChangedData(string UserId, FriendshipStatus? NewStatus, UserResponseDto User)
{
    public static FriendshipChangedData Create(string userId, FriendshipStatus? newStatus, UserResponseDto user = null)
    {
        user ??= new UserResponseDto { UserId = userId };

        // Normalize the DTO so receiving pages render the correct glyph/color for the new status.
        if (newStatus != null)
        {
            if (user.Friendship == null || user.Friendship.Status != newStatus)
            {
                user.Friendship = new Friendship
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserId = userId,
                    FriendId = CommonShared.UserId,
                    Status = newStatus.Value,
                    CreatedAt = DateTime.UtcNow
                };
            }
        }
        else user.Friendship = null;

        return new FriendshipChangedData(userId, newStatus, user);
    }
}
