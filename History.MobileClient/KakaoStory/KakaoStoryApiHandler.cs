#pragma warning disable SYSLIB0014 // Type or member is obsolete
using System.Net;
using System.Text;
using History.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.KakaoStory;

public partial class KakaoStoryApiHandler
{
    public static List<Cookie> Cookies { get; set; } = null;

    private static CookieContainer s_cookieContainer { get; set; } = null;
    private static string s_kakaoAppKey { get; set; } = "90c1434c4e8916a6ec5aa88109889601";
    private static DateTime s_emoticonCredentialUpdatedTime = DateTime.MinValue;
    private static AuthController s_emoticonCredential;

    public delegate Task<bool> ReloginRequired();

    public static ReloginRequired OnReloginRequired { get; set; }

    public static int MaxRetryCount { get; set; } = 15;

    public static void Init(CookieContainer cookieContainer, List<Cookie> cookies, string appKey)
    {
        s_cookieContainer = cookieContainer;
        if (!string.IsNullOrEmpty(appKey)) s_kakaoAppKey = appKey;
        Cookies = cookies;
    }
    public static async Task<ProfileData.ProfileObject> GetProfileFeed(string id, string from, bool noActivity = false)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + (!noActivity ? "?with=activities" : "");
        if (from != null)
            requestURI += "&since=" + from;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        ProfileData.ProfileObject obj = JsonConvert.DeserializeObject<ProfileData.ProfileObject>(response);
        return obj;
    }
    public static async Task<ProfileData.Profile> GetBiography(string id)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "/biography";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<ProfileData.Profile>(response);
    }

    private const string EmoticonListUrl = "https://api-item.kakao.com/api/sdk/items";
    private const string EmoticonUrl = "https://mk.kakaocdn.net/dna/emoticons/resources";
    private const string EmoticonAuthUrl = "https://api-item.kakao.com/api/sdk/config";

    private static readonly SemaphoreSlim s_emoticonCredentialSemaphore = new(1, 1);

    /// <summary>
    /// Refreshes the emoticon credential when it is older than one hour.
    /// The double-checked semaphore prevents concurrent refreshes when multiple
    /// posts load emoticons at once. Failures are swallowed so fire-and-forget
    /// callers never observe unhandled exceptions; GetEmoticonUrlSync then
    /// returns null and the "(이모티콘)" text placeholder is kept.
    /// </summary>
    public static async Task EnsureEmoticonCredentialAsync()
    {
        var hoursAfterLastEmoticonCredential = (DateTime.UtcNow - s_emoticonCredentialUpdatedTime).TotalHours;
        if (hoursAfterLastEmoticonCredential <= 1) return;

        try
        {
            await s_emoticonCredentialSemaphore.WaitAsync();

            hoursAfterLastEmoticonCredential = (DateTime.UtcNow - s_emoticonCredentialUpdatedTime).TotalHours;
            if (hoursAfterLastEmoticonCredential <= 1) return; // Refreshed by another caller while we waited.

            s_emoticonCredential = await GetEmoticonCredential();
            s_emoticonCredentialUpdatedTime = DateTime.UtcNow;
        }
        catch { }
        finally { s_emoticonCredentialSemaphore.Release(); }
    }

    /// <summary>
    /// Builds an emoticon image URL synchronously from the cached credential.
    /// Returns null when the credential is not available yet (call
    /// EnsureEmoticonCredentialAsync first); the caller then keeps the
    /// "(이모티콘)" text placeholder.
    /// </summary>
    public static string GetEmoticonUrlSync(string id, string resourceId)
    {
        if (s_emoticonCredential?.Auth == null) return null;

        var url = EmoticonUrl;
        url += $"/{id}/thum_{resourceId.PadLeft(3, '0')}.png";
        url += $"?credential={s_emoticonCredential.Auth.Credential}";
        url += $"&expires={s_emoticonCredential.Auth.Expires}";
        url += "&allow_referer=story.kakao.com";
        url += $"&signature={Uri.EscapeDataString(s_emoticonCredential.Auth.Signature)}";
        url += $"&path={s_emoticonCredential.Auth.Path}";
        return url;
    }
    private static async Task<AuthController> GetEmoticonCredential()
    {
        var client = new RestClient(EmoticonAuthUrl);
        var request = new RestRequest();
		Cookies.ForEach(x => request.AddCookie(x.Name, x.Value, x.Path, x.Domain));

		request.Method = Method.Get;

        request.AddHeader("authorization", $"KakaoAK {s_kakaoAppKey}");
        request.AddHeader("ka", $"sdk/1.14.0 os/javascript lang/ko-KR device/Win32 origin/https%3A%2F%2Fstory.kakao.com");
        request.AddHeader("js-origin", $"https://story.kakao.com/");
        var response = await client.ExecuteAsync(request);
        var data = JsonConvert.DeserializeObject<AuthController>(response.Content);
        return data;
    }

    public static async Task<EmoticonItems> GetEmoticonList()
    {
        var client = new RestClient(EmoticonListUrl);
        var request = new RestRequest();
		Cookies.ForEach(x => request.AddCookie(x.Name, x.Value, x.Path, x.Domain));

		request.Method = Method.Get;

        request.AddHeader("authorization", $"KakaoAK {s_kakaoAppKey}");
        request.AddHeader("ka", "sdk/1.14.0 os/javascript lang/ko-KR device/Win32 origin/https%3A%2F%2Fstory.kakao.com");
        request.AddHeader("js-origin", "https://story.kakao.com/");
        request.AddHeader("referer", "https://api-item.kakao.com/cors/");
        var response = await client.ExecuteAsync(request);
        var text = response.Content;
        var data = JsonConvert.DeserializeObject<EmoticonItems>(text);
        return data;
    }
		
    public static async Task<ProfileRelationshipData.ProfileRelationship> GetProfileRelationship(string id)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "?profile_only=true";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        ProfileRelationshipData.ProfileRelationship obj = JsonConvert.DeserializeObject<ProfileRelationshipData.ProfileRelationship>(response);
        return obj;
    }
    public static async Task<TimeLineData.TimeLine> GetFeed(string from = null)
    {
        string requestURI = "https://story.kakao.com/a/feeds";
        if (from != null)
            requestURI += "?since=" + from;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<TimeLineData.TimeLine>(response);
    }
    public static async Task HidePost(string id)
    {
        string requestURI = "https://story.kakao.com/a/feeds/" + id;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        await GetResponseFromRequest(webRequest);
    }
    public static async Task BlockProfile(string id, bool isUnblock)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "/feed_block";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, isUnblock ? "DELETE" : "POST");
        await GetResponseFromRequest(webRequest);
    }
    public static async Task<ProfileData.Profile> BanProfile(string id)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "/ban";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<ProfileData.Profile>(response);
    }
    public static async Task<ProfileData.Profile> UnbanProfile(string id)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "/ban";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<ProfileData.Profile>(response);
    }
    public static async Task<FriendData.Friends> GetFriends()
    {
        string requestURI = "https://story.kakao.com/a/friends/";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        var content = await GetResponseFromRequest(webRequest);
		return JsonConvert.DeserializeObject<FriendData.Friends>(content);
    }
    public static async Task<BookmarkData.Bookmarks> GetBookmarks(string id, string from)
    {
        string requestURI = "https://story.kakao.com/a/profiles/" + id + "/sections/bookmark";
        if (from != null)
            requestURI += $"?since={from}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<BookmarkData.Bookmarks>(response);
    }
    public static async Task<string> GetScrapData(string url)
    {
        string requestURI = "https://story.kakao.com/a/scraper?url=" + Uri.EscapeDataString(url);
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        return await GetResponseFromRequest(webRequest);
    }
    public static async Task<bool> SetActivityProfile(string id, string permission, bool enable_share, bool comment_all_writable, bool is_must_read)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id;

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");
        string postData = $"permission={permission}&enable_share={GetBoolString(enable_share)}&comment_all_writable={GetBoolString(comment_all_writable)}&is_must_read={GetBoolString(is_must_read)}";
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> MutePost(string id, bool mute)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id + "/mute_push";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, mute ? "POST" : "DELETE");
        string postData = $"push_mute={mute}";
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<List<ShareData.Share>> GetShares(PostData data, bool isUP, string from = null)
    {

        string requestURI = "https://story.kakao.com/a/activities/" + data.id + "/shares/";
        if (isUP)
            requestURI = "https://story.kakao.com/a/activities/" + data.id + "/sympathies/";

        if (from != null)
            requestURI += $"?since={from}";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<ShareData.Share>>(response);
    }
    public static async Task<List<Comment>> GetComments(string id, string since = null)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id + "/comments?lpp=30&order=desc";
        if (since != null)
            requestURI += "&since=" + since;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<Comment>>(response);
    }
    public static async Task<UserProfile.ProfileData> GetProfileData()
    {
        string requestURI = "https://story.kakao.com/a/settings/profile";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<UserProfile.ProfileData>(response);
    }
    public static async Task<List<DataType.Actor>> GetSpecificFriend(string id)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id + "/specific_friends";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<DataType.Actor>>(response);
    }
    public static async Task<List<CommentLikes>> GetCommentLikes(string postId, string commentID)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/comments/" + commentID + "/likes";
        string method = "GET";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);
        var response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<CommentLikes>>(response);
    }
    public static async Task<Comment> LikeComment(string postId, string commentID, bool isDelete)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/comments/" + commentID + "/likes";
        string method;
        if (isDelete == true)
            method = "DELETE";
        else
            method = "POST";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);
        var response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<Comment>(response);
    }
    public static async Task<bool> RequestFriend(string id, bool isDelete)
    {
        string requestURI;
        string key;

        if (isDelete == true)
        {
            requestURI = "https://story.kakao.com/a/invitations/cancel";
            key = "user_id";
        }
        else
        {
            requestURI = "https://story.kakao.com/a/invitations";
            key = "friend_id";
        }

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");

        string postData = $"{key}={id}&has_profile=true";
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> AcceptFriendRequest(string id, bool isDelete)
    {
        string requestURI;
        if (isDelete)
            requestURI = "https://story.kakao.com/a/invitations/ignore";
        else
            requestURI = "https://story.kakao.com/a/invitations/accept";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");

        string postData = $"inviter_id={id}&has_profile=true";
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> RequestFavorite(string id, bool isUnpin)
    {
        string requestURI = "https://story.kakao.com/a/friends/" + id + "/favorite";
        string method;
        if (isUnpin != true)
            method = "POST";
        else
            method = "DELETE";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);
        webRequest.Method = method;
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> PinPost(string id, bool isUnpin)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id + "/bookmark";
        string method;
        if (isUnpin != true)
            method = "POST";
        else
            method = "DELETE";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> SharePost(string postId, List<QuoteData> quoteDatas, string permission, bool commentable, List<string> with_ids, List<string> trust_ids)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/share";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");
        string textContent = Uri.EscapeDataString(JsonConvert.SerializeObject(quoteDatas).Replace("\"id\":null,", ""));

        string postData = "content=" + textContent
            + "&permission=" + permission + "&comment_all_writable=" + (commentable ? "true" : "false")
            + "&is_must_read=false&enable_share=true";

        if ((with_ids?.Count ?? 0) > 0)
            postData += "&with_tags=" + Uri.EscapeDataString(JsonConvert.SerializeObject(with_ids));
        if ((trust_ids?.Count ?? 0) > 0)
            postData += "&allowed_profile_ids=" + Uri.EscapeDataString(JsonConvert.SerializeObject(trust_ids));

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> UpPost(string postId, bool isDelete)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/sympathy";
        string method;
        if (isDelete)
            method = "DELETE";
        else
            method = "POST";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> LikePost(string postId, string emotion)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/like";
        string method;
        if (emotion == null)
            method = "DELETE";
        else
            method = "POST";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, method);

        string postData;
        if (emotion == null)
            postData = "";
        else
            postData = "emotion=" + emotion;

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<PostData> GetPost(string activityID)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + activityID;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "GET");
        string respResult = await GetResponseFromRequest(webRequest);

        PostData obj = null;
        if (respResult != null)
            obj = JsonConvert.DeserializeObject<PostData>(respResult);

        return obj;
    }
    public static async Task<bool> DeleteFriend(string id)
    {
        string requestURI = "https://story.kakao.com/a/friends/" + id;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> DeleteLike(string postId, string id)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/like";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");

        string postData = $"id={id}";
        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> DeleteBirthday()
    {
        string requestURI = "https://story.kakao.com/a/agreement/birth";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> SetProfileName(string name)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/name";
        string postData = $"name={Uri.EscapeDataString(name)}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> SetBirthday(DateTime date, bool isLeapType)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/birthday";
        string postData = $"birth={Uri.EscapeDataString(date.ToString("yyyyMMdd"))}&birth_type={Uri.EscapeDataString(isLeapType == true ? "-" : "+")}&birth_leap_type={isLeapType.ToString().ToLower()}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> SetGender(string gender, string permission)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/gender";
        string postData = $"gender={gender}&permission={permission}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> DeleteGender()
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/gender";
        string postData = $"gender=&permission=";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> SetStatusMessage(string message)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/status_message";
        string postData = $"status_message={Uri.EscapeDataString(message)}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        Stream writeStream = await webRequest.GetRequestStreamAsync();
        writeStream.Write(byteArray, 0, byteArray.Length);
        writeStream.Close();

        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<UserProfile.ProfileData> SetBackgroundImage(string imagePath)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/image/background";
        string postData = $"image_path={Uri.EscapeDataString(imagePath)}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        string response = await GetResponseFromRequest(webRequest, byteArray);
        return JsonConvert.DeserializeObject<UserProfile.ProfileData>(response);
    }
    public static async Task<UserProfile.ProfileData> SetProfileImage(string imagePath)
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/image/profile";
        string postData = $"image_path={Uri.EscapeDataString(imagePath)}";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        string response = await GetResponseFromRequest(webRequest, byteArray);
        return JsonConvert.DeserializeObject<UserProfile.ProfileData>(response);
    }
    public static async Task<UserProfile.ProfileData> DeleteProfileImage()
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/image/profile";
        string postData = "default_image_id=-1";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        string response = await GetResponseFromRequest(webRequest, byteArray);
        return JsonConvert.DeserializeObject<UserProfile.ProfileData>(response);
    }
    public static async Task<UserProfile.ProfileData> DeleteBackgroundImage()
    {
        string requestURI = "https://story.kakao.com/a/settings/profile/image/background";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");

        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<UserProfile.ProfileData>(response);
    }
    public static async Task<bool> SendMail(string content, string id, bool bomb, string imgURI = null)
    {
        string requestURI = "https://story.kakao.com/a/messages?_=" + ((long)DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds - 32400).ToString() + "11149";
        string objectStr = $"&object=%7B%22background%22%3A%7B%22type%22%3A%22color%22%2C%22value%22%3A{new Random().Next(10983816, 10983816)}%7D%7D";

        if (imgURI != null)
            objectStr = "";

        string postData = $"content={Uri.EscapeDataString("[{\"type\":\"text\",\"text\":\"" + content + "\"}]")}&bomb={bomb.ToString().ToLower()}" + objectStr + $"&receiver_id%5B%5D={id}&reference_id=";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");
        webRequest.Headers["Origin"] = "https://story.kakao.com";
        webRequest.Headers["Cache-Control"] = "no-cache";
        webRequest.Referer = "https://story.kakao.com/";

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<List<MailData.Mail>> GetMails(string since = null)
    {
        string requestURI = "https://story.kakao.com/a/messages/";
        if (since != null)
            requestURI += $"?since={since}";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "GET");

        return JsonConvert.DeserializeObject<List<MailData.Mail>>(await GetResponseFromRequest(webRequest)); ;
    }
    public static async Task<MailData.MailDetail> GetMailDetail(string id)
    {
        string requestURI = "https://story.kakao.com/a/messages/" + id;

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "GET");

        return JsonConvert.DeserializeObject<MailData.MailDetail>(await GetResponseFromRequest(webRequest)); ;
    }
    public static async Task<bool> DeleteMail(string id)
    {
        string requestURI = "https://story.kakao.com/a/messages/" + id;

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");

        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<NotificationStatus> GetNotificationStatus()
    {
        var milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        string requestURI = $"https://story.kakao.com/a/notifications/new_count?notice_since=&_={milliseconds}000";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<NotificationStatus>(response);
    }
    public static async Task<List<Notification>> GetNotifications()
    {
        string requestURI = "https://story.kakao.com/a/notifications";
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<Notification>>(response);
    }
    public static async Task<bool> DeletePost(string id)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + id;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<bool> ReplyToPost(string postId, string text, List<QuoteData> quoteDatas, UploadedImageProp img = null)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/comments";
        string textContent = Uri.EscapeDataString(JsonConvert.SerializeObject(quoteDatas).Replace("\"id\":null,", ""));

        string postData;
        string imageData2 = "";

        if (img != null)
        {
            imageData2 = "(Image) ";
            string imageData = "{\"media_path\":\"" + img.access_key + "/" + img.info.original.filename + "?width=" + img.info.original.width + "&height=" + img.info.original.height + "&avg=" + img.info.original.avg + "\",\"type\":\"image\",\"text\":\"(Image) \"},";
            textContent = textContent.Insert(3, Uri.EscapeDataString(imageData));
        }

        postData = "text=" + Uri.EscapeDataString(imageData2 + text) + "&decorators=" + textContent;

        postData = postData.Replace("%20", "+");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "POST");
        return await GetResponseFromRequest(webRequest, byteArray) != null;
    }
    public static async Task<bool> DeleteComment(string commentID, string postId)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/comments/" + commentID;
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "DELETE");
        return await GetResponseFromRequest(webRequest) != null;
    }
    public static async Task<Comment> EditComment(Comment comment, string postId, List<QuoteData> quoteDatas, string text)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + postId + "/comments/" + comment.id + "/content";

        string textContent = Uri.EscapeDataString(JsonConvert.SerializeObject(quoteDatas).Replace("\"id\":null,", ""));
        string imageData2 = "";
        foreach (QuoteData qdata in comment.decorators)
        {
            if (qdata.media_path != null)
            {
                imageData2 = "(Image) ";
                string imageData = JsonConvert.SerializeObject(qdata, Formatting.None, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                textContent = textContent.Insert(3, Uri.EscapeDataString(imageData));
            }
        }
        string postData = "text=" + Uri.EscapeDataString(imageData2 + text);
        postData += "&decorators=" + textContent;

        postData = postData.Replace("%20", "+");

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI, "PUT");
        var response = await GetResponseFromRequest(webRequest, byteArray);
        return JsonConvert.DeserializeObject<Comment>(response);
    }
    public static async Task<List<ShareData.Share>> GetShares(bool isUP, PostData data, string from)
    {

        string requestURI = "https://story.kakao.com/a/activities/" + data.id + "/shares/";
        if (isUP)
            requestURI = "https://story.kakao.com/a/activities/" + data.id + "/sympathies/";

        if (from != null)
            requestURI += $"?since={from}";

        HttpWebRequest webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<ShareData.Share>>(response);
    }
    public static async Task<List<ShareData.Share>> GetLikes(PostData data, string from)
    {
        string requestURI = "https://story.kakao.com/a/activities/" + data.id + "/likes/";
        if (from != null)
            requestURI += "?since=" + from;
        var webRequest = GenerateDefaultProfile(requestURI);
        string response = await GetResponseFromRequest(webRequest);
        return JsonConvert.DeserializeObject<List<ShareData.Share>>(response);
    }
    /// <summary>
    /// Sends the request (optionally writing <paramref name="body"/> once), returning the
    /// response body. On 401 the request is held until OnReloginRequired completes, then
    /// retried with the same body. Other failures retry up to MaxRetryCount times.
    /// <paramref name="configure"/> re-applies request-specific headers on every retry.
    /// </summary>
    private static async Task<string> GetResponseFromRequest(HttpWebRequest webRequest, byte[] body = null, int count = 0, Action<HttpWebRequest> configure = null)
    {
        try
        {
            if (body != null)
            {
                Stream writeStream = await webRequest.GetRequestStreamAsync();
                writeStream.Write(body, 0, body.Length);
                writeStream.Close();
            }

            var readStream = await webRequest.GetResponseAsync();
            var respReader = (readStream?.GetResponseStream()) ?? throw new Exception("Network Error!");
            using var reader = new StreamReader(respReader);
            string respResult = await reader.ReadToEndAsync();

            respReader.Close();
            readStream.Close();
            return respResult;
        }
        catch (WebException exception)
        {
            int statusCode = -1;
            var statusCodeObject = exception.Response as HttpWebResponse;
            if (statusCodeObject?.StatusCode != null) statusCode = (int)statusCodeObject.StatusCode;

            if (statusCode == 401)
            {
                var success = await OnReloginRequired?.Invoke();
                if (!success) return null;
                var newRequest = GenerateDefaultProfile(webRequest.RequestUri.ToString(), webRequest.Method);
                configure?.Invoke(newRequest);
                return await GetResponseFromRequest(newRequest, body, ++count, configure);
            }
            else if (statusCode == 403 || statusCode == 404) return null;
            else if (count < MaxRetryCount)
            {
                var newRequest = GenerateDefaultProfile(webRequest.RequestUri.ToString(), webRequest.Method);
                configure?.Invoke(newRequest);
                return await GetResponseFromRequest(newRequest, body, ++count, configure);
            }
        }
        catch (Exception)
        {
            if (count < MaxRetryCount)
            {
                var newRequest = GenerateDefaultProfile(webRequest.RequestUri.ToString(), webRequest.Method);
                configure?.Invoke(newRequest);
                return await GetResponseFromRequest(newRequest, body, ++count, configure);
            }
        }
        return null;
    }

    /// <summary>
    /// Writes multi part HTTP POST request. Author : Farhan Ghumra
    /// </summary>
    private static void WriteMultipartForm(Stream s, string boundary, Dictionary<string, string> data, string fileName, string fileContentType, Stream fileStream)
    {
        // The first boundary
        byte[] boundarybytes = Encoding.UTF8.GetBytes("--" + boundary + "\r\n");
        // the last boundary.
        byte[] trailer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
        // the form data, properly formatted
        // Content-Disposition: form-data; name="file_1"; filename="waifu.png"
        //Content - Type: image / png

        string formdataTemplate = "Content-Disposition; name=\"{0}\"\r\n\r\n{1}";
        // the form-data file upload, properly formatted
        string fileheaderTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\";\r\nContent-Type: {2}\r\n\r\n";

        // Added to track if we need a CRLF or not.
        bool bNeedsCRLF = false;

        if (data != null)
        {
            foreach (string key in data.Keys)
            {
                // if we need to drop a CRLF, do that.
                if (bNeedsCRLF)
                    WriteToStream(s, "\r\n");

                // Write the boundary.
                WriteToStream(s, boundarybytes);

                // Write the key.
                WriteToStream(s, string.Format(formdataTemplate, key, data[key]));
                bNeedsCRLF = true;
            }
        }

        // If we don't have keys, we don't need a crlf.
        if (bNeedsCRLF)
            WriteToStream(s, "\r\n");

        WriteToStream(s, boundarybytes);
        WriteToStream(s, string.Format(fileheaderTemplate, "file_1", fileName, fileContentType));
        // Write the file data to the stream.
        byte[] buffer = new byte[4096];
        while ((fileStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            WriteToStream(s, buffer);
        }
        fileStream.Dispose();
        WriteToStream(s, trailer);
    }

    /// <summary>
    /// Writes string to stream. Author : Farhan Ghumra
    /// </summary>
    private static void WriteToStream(Stream s, string txt)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(txt);
        s.Write(bytes, 0, bytes.Length);
    }
    private static void WriteToStream(Stream s, byte[] bytes)
    {
        s.Write(bytes, 0, bytes.Length);
    }
    public static async Task WritePost(List<QuoteData> quoteDatas, MediaData mediaData, string permission, bool isCommentable, bool isSharable, List<string> with_ids, List<string> trust_ids, string scrapDataString = null, bool isEdit = false, List<string> editOldMediaPaths = null, string editPostId = null)
    {
        editOldMediaPaths ??= [];

        string commentable = isCommentable ? "true" : "false";
        string sharable = isSharable ? "true" : "false";
        string textContent = Uri.EscapeDataString(JsonConvert.SerializeObject(quoteDatas, Formatting.None, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        }));
        StringBuilder postDataBuilder = new();
        postDataBuilder.Append("permission=" + permission + "&comment_all_writable=" + commentable + "&is_must_read=false&enable_share=" + sharable);
        postDataBuilder.Append("&content=" + textContent);

        if ((with_ids?.Count ?? 0) > 0)
            postDataBuilder.Append("&with_tags=" + Uri.EscapeDataString(JsonConvert.SerializeObject(with_ids)));
        if ((trust_ids?.Count ?? 0) > 0)
            postDataBuilder.Append("&allowed_profile_ids=" + Uri.EscapeDataString(JsonConvert.SerializeObject(trust_ids)));

        string mediaText = JsonConvert.SerializeObject(mediaData);
        if (mediaText != null && mediaData != null)
        {
            postDataBuilder.Append("&" + Uri.EscapeDataString("media") + "=" + Uri.EscapeDataString(mediaText));
        }
        foreach (string mediaPath in editOldMediaPaths)
        {
            postDataBuilder.Append("&" + Uri.EscapeDataString("old_media_path[]") + "=" + Uri.EscapeDataString(mediaPath));
        }

        if (scrapDataString != null)
        {
            postDataBuilder.Append("&scrap_content=" + Uri.EscapeDataString(scrapDataString));
        }

        string postData = postDataBuilder.ToString();

        byte[] byteArray = Encoding.UTF8.GetBytes(postData);

        string requestURI = "https://story.kakao.com/a/activities";
        if (isEdit)
            requestURI = "https://story.kakao.com/a/activities/" + editPostId + "/content";

        // WritePost uses custom headers (fixed X-Kakao-VC, authority) that must survive retries.
        void Configure(HttpWebRequest request)
        {
            request.Headers["X-Kakao-VC"] = "185412afe1da9580e67f";
            request.Headers["authority"] = "story.kakao.com";
            request.Referer = "https://story.kakao.com";
        }

        var request = GenerateDefaultProfile(requestURI, isEdit ? "PUT" : "POST");
        Configure(request);
        await GetResponseFromRequest(request, byteArray, configure: Configure);
    }

    private static async Task<string> GetUploadUrl(bool isImage)
    {
        using var client = new RestClient("https://story.kakao.com/a/web/media/upload-url");
        var request = new RestRequest();
        request.Method = Method.Post;
        request.CookieContainer = s_cookieContainer;
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Accept-Encoding", "gzip, deflate, br");
        request.AddHeader("Accept-Language", "ko");
        request.AddHeader("Origin", "https://story.kakao.com");
        request.AddHeader("Referer", "https://story.kakao.com/");
        request.AddHeader("Sec-Ch-Ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Microsoft Edge\";v=\"122\"");
        request.AddHeader("Sec-Ch-Ua-Mobile", "?0");
        request.AddHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
        request.AddHeader("Sec-Fetch-Dest", "empty");
        request.AddHeader("Sec-Fetch-Mode", "cors");
        request.AddHeader("Sec-Fetch-Site", "same-origin");
        request.AddHeader("X-Kakao-Apilevel", "49");
        request.AddHeader("X-Kakao-Deviceinfo", "web:d;-;-");
        request.AddHeader("X-Kakao-Vc", "185412afe1da9580e67f");
        request.AddHeader("X-Requested-With", "XMLHttpRequest");

        request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
        if (isImage)
        {
            request.AddParameter("config", "/web/webstory-img");
            request.AddParameter("upload_url", "https://up-api-kage-4story.kakao.com");
        }
        else
        {
            request.AddParameter("config", "/web/webstory-video");
            request.AddParameter("upload_url", "https://up-api-kage-4story-video.kakao.com");
        }

        var response = await client.ExecuteAsync(request);
        var content = response.Content;
        var jsonObject = JObject.Parse(content);
        var uploadUrl = jsonObject["url"].ToString();
        return uploadUrl;
    }

    private static async Task<string> GetVideoCheckUrl(string key)
    {
        using var client = new RestClient("https://story.kakao.com/a/web/media/wcheck-url");
        var request = new RestRequest();
        request.Method = Method.Post;
        request.CookieContainer = s_cookieContainer;
        request.AddHeader("Accept", "application/json");
        request.AddHeader("Accept-Encoding", "gzip, deflate, br");
        request.AddHeader("Accept-Language", "ko");
        request.AddHeader("Origin", "https://story.kakao.com");
        request.AddHeader("Referer", "https://story.kakao.com/");
        request.AddHeader("Sec-Ch-Ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Microsoft Edge\";v=\"122\"");
        request.AddHeader("Sec-Ch-Ua-Mobile", "?0");
        request.AddHeader("Sec-Ch-Ua-Platform", "\"Windows\"");
        request.AddHeader("Sec-Fetch-Dest", "empty");
        request.AddHeader("Sec-Fetch-Mode", "cors");
        request.AddHeader("Sec-Fetch-Site", "same-origin");
        request.AddHeader("X-Kakao-Apilevel", "49");
        request.AddHeader("X-Kakao-Deviceinfo", "web:d;-;-");
        request.AddHeader("X-Kakao-Vc", "185412afe1da9580e67f");
        request.AddHeader("X-Requested-With", "XMLHttpRequest");

        request.AddHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
        request.AddParameter("upload_url", "https://up-api-kage-4story-video.kakao.com");
        request.AddParameter("key", key);

        var response = await client.ExecuteAsync(request);
        var content = response.Content;
        var jsonObject = JObject.Parse(content);
        var uploadUrl = jsonObject["url"].ToString();
        return uploadUrl;
    }

    public static async Task<string> UploadImage(string filepath)
    {
        var result = await UploadImageProp(filepath);
        return result.access_key + "/" + result.info.original.filename + "?width=" + result.info.original.width + "&height=" + result.info.original.height + "&avg=" + result.info.original.avg;
    }

    public static async Task<UploadedImageProp> UploadImageProp(string filepath)
    {
        string filename = Path.GetFileName(filepath);
        StreamReader fileStream = new StreamReader(filepath);

        string requestURI = await GetUploadUrl(true);

        HttpWebRequest request = WebRequest.CreateHttp(requestURI);
        request.Method = "POST";
        string boundary = "----" + DateTime.Now.Ticks.ToString("x");
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.CookieContainer = s_cookieContainer;

        request.Headers["X-Kakao-DeviceInfo"] = "web:d;-;-";
        request.Headers["X-Kakao-ApiLevel"] = "45";
        request.Headers["X-Requested-With"] = "XMLHttpRequest";
        request.Headers["X-Kakao-VC"] = "1b242cf8fa50f1f96765";
        request.Headers["Cache-Control"] = "max-age=0";
        request.Headers["Accept-Encoding"] = "gzip, deflate, br";
        request.Headers["Accept-Language"] = "ko-KR,ko;q=0.8,en-US;q=0.6,en;q=0.4";

        request.Headers["DNT"] = "1";

        request.Headers["authority"] = "story.kakao.com";
        request.Referer = "https://story.kakao.com";
        request.KeepAlive = true;
        request.UseDefaultCredentials = true;
        request.Host = "up-api-kage-4story.kakao.com";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        request.Accept = "*/*";
        request.AutomaticDecompression = DecompressionMethods.GZip;

        Stream writeStream = await request.GetRequestStreamAsync();

        WriteMultipartForm(writeStream, boundary, null, filename, MimeTypes.GetMimeType(filename), fileStream.BaseStream);

        var readStream = await request.GetResponseAsync();
        var respReader = readStream.GetResponseStream();

        string respResult = await (new StreamReader(respReader, Encoding.UTF8)).ReadToEndAsync();
        respReader.Close();

        UploadedImageProp result = JsonConvert.DeserializeObject<UploadedImageProp>(respResult);
        return result;
    }

    public static async Task<string> UploadVideo(string filePath)
    {
        StreamReader fileStream = new StreamReader(filePath);

        string requestURI = await GetUploadUrl(false);

        string boundary = "----" + DateTime.Now.Ticks.ToString("x");

        HttpWebRequest request = WebRequest.CreateHttp(requestURI);
        request.Method = "POST";
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.CookieContainer = s_cookieContainer;

        request.Headers["X-Kakao-DeviceInfo"] = "web:d;-;-";
        request.Headers["X-Kakao-ApiLevel"] = "46";
        request.Headers["X-Requested-With"] = "XMLHttpRequest";
        request.Headers["X-Kakao-VC"] = "185412afe1da9580e67f";
        request.Headers["Cache-Control"] = "max-age=0";
        request.Headers["Accept-Encoding"] = "gzip, deflate, br";
        request.Headers["Accept-Language"] = "ko-KR,ko;q=0.8,en-US;q=0.6,en;q=0.4";

        request.Headers["DNT"] = "1";

        request.Headers["authority"] = "story.kakao.com";
        request.Referer = "https://story.kakao.com";
        request.KeepAlive = true;
        request.UseDefaultCredentials = true;
        request.Host = "up-api-kage-4story-video.kakao.com";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        request.Accept = "*/*";
        request.AutomaticDecompression = DecompressionMethods.GZip;

        Stream writeStream = await request.GetRequestStreamAsync();

        WriteMultipartForm(writeStream, boundary, null, Path.GetFileName(filePath), MimeTypes.GetMimeType(filePath), fileStream.BaseStream);
        fileStream.Close();

        var readStream = await request.GetResponseAsync();
        var respReader = readStream.GetResponseStream();

        string respResult = await (new StreamReader(respReader, Encoding.UTF8)).ReadToEndAsync();
        respReader.Close();

        var videoData = JsonConvert.DeserializeObject<VideoData.Video>(respResult);
        return videoData.access_key;
    }

    private static async Task<bool> WaitForMetaVideoFinish(string access_key, int retryCount = 0)
    {
        string requestURI = "https://story.kakao.com/a/kage/video/dn/" + access_key + "/meta.json";
        HttpWebRequest request = WebRequest.CreateHttp(requestURI);

        request.Method = "GET";

        request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
        request.CookieContainer = s_cookieContainer;

        request.Headers["X-Kakao-DeviceInfo"] = "web:d;-;-";
        request.Headers["X-Kakao-ApiLevel"] = "49";
        request.Headers["X-Requested-With"] = "XMLHttpRequest";
        request.Headers["X-Kakao-VC"] = "185412afe1da9580e67f";
        request.Headers["Cache-Control"] = "max-age=0";

        request.Headers["Accept-Encoding"] = "gzip, deflate, br";
        request.Headers["Accept-Language"] = "ko";

        request.Headers["DNT"] = "1";

        request.Headers["authority"] = "story.kakao.com";
        request.Referer = "https://story.kakao.com/";
        request.KeepAlive = true;
        request.UseDefaultCredentials = true;
        request.Host = "story.kakao.com";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        request.Accept = "application/json";

        try
        {
            var readStream = await request.GetResponseAsync();
            var respReader = readStream.GetResponseStream();
            using var reader = new StreamReader(respReader, Encoding.UTF8);
            await reader.ReadToEndAsync();
            respReader.Close();
            return true;
        }
        catch (WebException e)
        {
            int statusCode = -1;
            var statusCodeObject = e.Response as HttpWebResponse;
            if (statusCodeObject?.StatusCode != null) statusCode = (int)statusCodeObject.StatusCode;

            if (statusCode == 403) return false;
            else if (statusCode == 404) return false;
            else if (statusCode == 401) return await WaitForMetaVideoFinish(access_key, ++retryCount);
            else
            {
                if (retryCount < MaxRetryCount)
                    return await WaitForMetaVideoFinish(access_key, ++retryCount);
            }
        }
        return false;
    }
    public static async Task<bool> WaitForVideoUploadFinish(string access_key, int retryCount = 0)
    {
        string requestURI = await GetVideoCheckUrl(access_key);
        HttpWebRequest request = WebRequest.CreateHttp(requestURI);

        request.Method = "POST";

        request.CookieContainer = s_cookieContainer;

        request.Headers["X-Kakao-DeviceInfo"] = "web:d;-;-";
        request.Headers["X-Kakao-ApiLevel"] = "49";
        request.Headers["X-Requested-With"] = "XMLHttpRequest";
        request.Headers["X-Kakao-VC"] = "4736c40740682cf57eb2";

        request.Headers["Accept"] = "application/json";
        request.Headers["Accept-Encoding"] = "gzip, deflate, br, zstd";
        request.Headers["Accept-Language"] = "ko";

        request.Headers["DNT"] = "1";

        request.Headers["Origin"] = "https://story.kakao.com";

        request.AutomaticDecompression = DecompressionMethods.GZip;
        request.Headers["authority"] = "story.kakao.com";
        request.Referer = "https://story.kakao.com/";
        request.KeepAlive = true;
        request.UseDefaultCredentials = true;
        request.Host = "story.kakao.com";
        request.UserAgent = "MozillaMozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36";
        request.Accept = "application/json";

        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            var respReader = response.GetResponseStream();
            using var reader = new StreamReader(respReader);
            string respResult = reader.ReadToEnd();
            respReader.Close();
            response.Close();
            VideoData.Percent pecrentData = JsonConvert.DeserializeObject<VideoData.Percent>(respResult);
            if (pecrentData.code == 200 && pecrentData.percent == 100)
                return await WaitForMetaVideoFinish(access_key);
            else
                await Task.Delay(500);
            return await WaitForVideoUploadFinish(access_key);
        }
        catch (WebException e)
        {
            int statusCode = -1;
            var statusCodeObject = e.Response as HttpWebResponse;
            var respReader = statusCodeObject.GetResponseStream();
            using var reader = new StreamReader(respReader);
            string respResult = reader.ReadToEnd();
            if (statusCodeObject?.StatusCode != null) statusCode = (int)statusCodeObject.StatusCode;

            if (statusCode == 403) return false;
            else if (statusCode == 404) return false;
            else if (statusCode == 401) return await WaitForVideoUploadFinish(access_key, ++retryCount);
            else
            {
                if (retryCount < MaxRetryCount)
                    return await WaitForVideoUploadFinish(access_key, ++retryCount);
            }
        }
        return false;
    }

    private static string GetBoolString(bool src)
    {
        return src ? "true" : "false";
    }
    private static HttpWebRequest GenerateDefaultProfile(string requestURI, string method = "GET")
    {
        HttpWebRequest webRequest = WebRequest.CreateHttp(requestURI);
        webRequest.Method = method.ToUpper();
        webRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";

        webRequest.CookieContainer = s_cookieContainer;

        webRequest.Headers["X-Kakao-DeviceInfo"] = "web:d;-;-";
        webRequest.Headers["X-Kakao-ApiLevel"] = "49";
        webRequest.Headers["X-Requested-With"] = "XMLHttpRequest";
        webRequest.Headers["X-Kakao-VC"] = Guid.NewGuid().ToString().ToLower()[..20];
        webRequest.Headers["Cache-Control"] = "max-age=0";

        webRequest.Headers["Accept-Encoding"] = "gzip, deflate, br";
        webRequest.Headers["Accept-Language"] = "ko";

        webRequest.Headers["DNT"] = "1";

        webRequest.Referer = "https://story.kakao.com/";
        webRequest.KeepAlive = true;
        webRequest.UseDefaultCredentials = true;
        webRequest.Host = "story.kakao.com";
        webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        webRequest.Accept = "application/json";

        webRequest.AutomaticDecompression = DecompressionMethods.GZip;
        webRequest.Date = DateTime.Now;

        return webRequest;
    }
}
#pragma warning restore SYSLIB0014 // Type or member is obsolete