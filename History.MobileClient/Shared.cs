using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Messages;
using History.MobileClient.ShellTabBarBadge;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient;

public static class Shared
{
    public static ApiHandler ApiHandler { get; set; } = ApiHandler.Public;
    public static string UserId { get; set; }
    public static Rank MyRank { get; set; }
    public static List<UserResponseDto> Friends { get; set; }
    public static List<FriendData.Profile> KakaoFriends { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
    public static string KakaoUserId { get; set; }

    public static int HistoryUnreadNotificationCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    public static int KakaoStoryUnreadNotificationCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    public static int HistoryUnreadMailCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    public static int KakaoStoryUnreadMailCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    public static int HistoryPendingFriendRequestCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    public static int KakaoStoryPendingFriendRequestCount
    {
        get;
        set
        {
            field = value;
            OnBadgeCountChanged();
        }
    }

    private static int s_lastBadgeTotalCount = -1;
    private static int s_lastFriendRequestCount = -1;

    // Notifies the pill badge subscribers (the list pages) whenever a badge
    // count changes. The tab bar badge update is skipped when the summed total
    // is unchanged, so the pill badges need their own notification.
    private static void OnBadgeCountChanged()
    {
        UpdateTabBadge();
        WeakReferenceMessenger.Default.Send(new BadgeCountsChangedMessage());
    }

    // The pollers run on background threads, so the badge view update must be
    // marshalled to the main thread. The badge API is a no-op when no Shell is up.
    private static void UpdateTabBadge()
    {
        // Kakao Story counts are only summed into the badges when their badge
        // settings are enabled; the raw counts stay tracked regardless.
        var isKakaoStoryNotificationBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryNotificationBadgeEnabled") ?? true;
        var isKakaoStoryMailBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryMailBadgeEnabled") ?? true;
        var isKakaoStoryFriendRequestBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryFriendRequestBadgeEnabled") ?? true;

        var totalCount = HistoryUnreadNotificationCount + HistoryUnreadMailCount;
        if (isKakaoStoryNotificationBadgeEnabled) totalCount += KakaoStoryUnreadNotificationCount;
        if (isKakaoStoryMailBadgeEnabled) totalCount += KakaoStoryUnreadMailCount;
        if (totalCount != s_lastBadgeTotalCount)
        {
            s_lastBadgeTotalCount = totalCount;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (totalCount > 0) TabBarBadge.Set(1, totalCount.ToString(), textColor: Colors.White, color: Colors.Red);
                else TabBarBadge.Set(1, style: BadgeStyle.Hidden);
            });
        }

        var friendRequestCount = HistoryPendingFriendRequestCount;
        if (isKakaoStoryFriendRequestBadgeEnabled) friendRequestCount += KakaoStoryPendingFriendRequestCount;
        if (friendRequestCount == s_lastFriendRequestCount) return; // Skip identical renders (1s Kakao Story cadence).
        s_lastFriendRequestCount = friendRequestCount;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (friendRequestCount > 0) TabBarBadge.Set(2, friendRequestCount.ToString(), textColor: Colors.White, color: Colors.Red);
            else TabBarBadge.Set(2, style: BadgeStyle.Hidden);
        });
    }

    // Forces the next UpdateTabBadge call to re-apply the badges. Called after
    // login because pre-login Set calls are dropped (no Shell yet) and the
    // change guard would otherwise skip the re-application.
    public static void ResetTabBadgeCache()
    {
        s_lastBadgeTotalCount = -1;
        s_lastFriendRequestCount = -1;
    }

    // Re-applies the badges after a badge setting toggle; the change guard
    // would otherwise skip the re-render since the raw counts did not change.
    // The pill badge subscribers are notified as well so the Kakao Story pill
    // badges reflect the toggled setting right away.
    public static void RefreshTabBadges()
    {
        ResetTabBadgeCache();
        UpdateTabBadge();
        WeakReferenceMessenger.Default.Send(new BadgeCountsChangedMessage());
    }
}
