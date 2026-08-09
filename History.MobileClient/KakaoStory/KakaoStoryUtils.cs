using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using Newtonsoft.Json;
using System.Text;
using UraniumUI.Icons.FontAwesome;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.KakaoStory;

public static class KakaoStoryUtils
{
    // Guide shown when entering the Kakao Story-only write mode and when blocking a
    // post without any user mention. Kakao Story-only writing exists solely to mention
    // Kakao Story users that History posts cannot reference.
    public const string KakaoOnlyWriteGuideMessage = 
        "카카오스토리에만 게시글을 작성하는 기능은 히스토리 게시글 작성으로는 불가능한 카카오스토리 사용자를 언급하기 위한 기능으로, 게시글에 사용자 언급이 존재하지 않으면 게시글 작성이 차단됩니다. 히스토리 활성화를 위한 조치로써 해당 용도가 아닌 게시글 작성은 히스토리탭에서 게시글을 작성하신 뒤 \"카카오스토리에도 게시글 작성\" 옵션을 글 작성 메뉴 우측 하단의 펼침 메뉴에서 활성화하고 작성해주세요";

    // Kakao Story emotions map to the History reaction visuals (glyph + color).
    // Emotion strings: like(좋아요), good(멋져요), pleasure(기뻐요), sad(슬퍼요), cheerup(힘내요).
    public static (string Glyph, Color Color) GetEmotionVisual(string emotion)
    {
        return emotion switch
        {
            "like" => (Solid.Heart, Color.FromRgb(0xEB, 0x55, 0x27)),
            "good" => (Solid.Star, Color.FromRgb(0xBB, 0xCC, 0x29)),
            "pleasure" => (Solid.FaceSmile, Color.FromRgb(0xFF, 0xC1, 0x00)),
            "sad" => (Solid.Droplet, Color.FromRgb(0x00, 0x9F, 0xB2)),
            "cheerup" => (Solid.Bolt, Color.FromRgb(0xA0, 0x61, 0xB1)),
            _ => (Solid.Heart, Color.FromRgb(0xEB, 0x55, 0x27))
        };
    }

    private static bool s_isRelogging;

    /// <summary>
    /// Relogin entry point used by KakaoStoryApiHandler when a request returns 401.
    /// Presents the login modal (or auto-fill) and updates the stored cookies.
    /// Returns true when a valid session is available afterwards.
    /// Re-entrancy guard: the cookie validation inside EnsureLoggedInAsync also performs
    /// API requests, which 401 again and would otherwise recurse into this method forever.
    /// Nested invocations return false and let the outer relogin flow show the login page.
    /// </summary>
    public static async Task<bool> ReLoginAsync()
    {
        if (s_isRelogging) return false;
        s_isRelogging = true;
        try
        {
            var success = await EnsureLoggedInAsync(App.TopPage);
            if (success)
            {
                // Refresh the cached user id after relogin so post action sheets stay accurate.
                await SaveCurrentUserAsync();
                // Allow the background 401 flow to notify about the next expired session.
                KakaoStoryNotificationPoller.ResetSessionExpiredNotification();
            }
            return success;
        }
        finally { s_isRelogging = false; }
    }

    /// <summary>
    /// Validates the saved SDK tokens (KAuth) and, when they are missing/expired,
    /// presents the KakaoStoryLoginPage modal. Returns true when a valid session
    /// is available afterwards.
    /// </summary>
    public static async Task<bool> EnsureLoggedInAsync(Page hostPage)
    {
        if (await KakaoStoryApiHandler.EnsureKAuthTokenAsync() != null)
        {
            try
            {
                Shared.KakaoFriends = (await KakaoStoryApiHandler.GetFriends())?.profiles;
                await SaveCurrentUserAsync();
                _ = KakaoStoryApiHandler.EnsureEmoticonCredentialAsync(); // Warm up so first emoticons render immediately.
                return true;
            }
            catch { }
        }

        var savedEmail = Configuration.GetValue<string>("KakaoStoryEmail");
        var savedEncryptedPassword = Configuration.GetValue<string>("KakaoStoryPassword");
        if (savedEmail == null || savedEncryptedPassword == null)
        {
            var useAutoFill = await hostPage.DisplayAlertAsync("자동 입력", "세션이 만료되어 로그인이 필요합니다. 카카오스토리 로그인 정보를 저장하여 자동 입력하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (useAutoFill)
            {
                var email = await hostPage.DisplayPromptAsync("이메일 입력", "카카오 계정 이메일을 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "이메일", -1, Keyboard.Email);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var password = await hostPage.DisplayPromptAsync("비밀번호 입력", "카카오 계정 비밀번호를 입력해주세요.", Constants.PromptOk, Constants.PromptCancel, "비밀번호", -1, Keyboard.Password);
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        var encryptedPassword = AesCryptoHelper.Encrypt(password, Constants.KakaoStoryCredentialEncryptionKey);
                        Configuration.SetValue("KakaoStoryEmail", email);
                        Configuration.SetValue("KakaoStoryPassword", encryptedPassword);
                    }
                }
            }
        }

        var kakaoStoryLoginPage = new KakaoStoryLoginPage();
        await App.PushModalAsync(kakaoStoryLoginPage);

        var isLoggedIn = await kakaoStoryLoginPage.GetResultAsync();
        if (!isLoggedIn) return false;

        await SaveCurrentUserAsync();
        _ = KakaoStoryApiHandler.EnsureEmoticonCredentialAsync(); // Warm up so first emoticons render immediately.
        KakaoStoryNotificationPoller.ResetSessionExpiredNotification();
        return true;
    }

    /// <summary>
    /// Builds contents with Kakao Story emoticons rendered as sticker view models
    /// (Referer-signed images). Text decorators are batched into
    /// TextTypeContentsViewModel; emoticons whose signed URL cannot be resolved
    /// keep the "(이모티콘)" text placeholder. Returns null when the input has no
    /// emoticon decorators — the caller then keeps its existing contents.
    /// </summary>
    public static async Task<List<IContentViewModel>> BuildEmoticonContentsAsync(List<QuoteData> quoteDatas, PostType postType)
    {
        if (quoteDatas == null || !quoteDatas.Any(x => x.type == "emoticon")) return null;

        await KakaoStoryApiHandler.EnsureEmoticonCredentialAsync();

        var contents = new List<IContentViewModel>();
        var textBatch = new List<QuoteData>();
        foreach (var data in quoteDatas)
        {
            if (data.type == "emoticon")
            {
                if (textBatch.Count > 0)
                {
                    contents.Add(new TextTypeContentsViewModel(textBatch, postType));
                    textBatch = [];
                }

                var emoticonUrl = KakaoStoryApiHandler.GetEmoticonUrlSync(data.item_id, data.resource_id.ToString());
                if (emoticonUrl == null) contents.Add(new TextTypeContentsViewModel([new QuoteData { type = "text", text = "(이모티콘) " }], postType));
                else contents.Add(new StickerContentViewModel(emoticonUrl, postType));
            }
            else textBatch.Add(data);
        }
        if (textBatch.Count > 0) contents.Add(new TextTypeContentsViewModel(textBatch, postType));

        return contents;
    }

    /// <summary>
    /// Creates the post view model for a Kakao Story feed item, unwrapping bundled feeds
    /// (share/UP activities) into the shared post/repost surfaces (WPF pattern):
    /// - bundled_feed.type == "up"    -> render the original activity as a repost card.
    /// - bundled_feed.type == "share" -> inject the original activity into activities[0].@object
    ///                                    so the shared card renders the original post.
    /// Returns null when the post author is banned (relation.ban == "A") so callers skip it.
    /// </summary>
    public static BasePostViewModel CreatePostViewModel(PostData postData)
    {
        if (postData.actor?.relation?.ban == "A") return null;

        var bundledFeed = postData.bundled_feed;
        if (postData.verb == "bundled_feed" && bundledFeed != null)
        {
            // The repost card renders the original activity's content, so the original
            // author is also checked for a ban (relation.ban == "A").
            if (bundledFeed.type == "up" && bundledFeed.original_activity != null)
            {
                if (bundledFeed.original_activity.actor?.relation?.ban == "A") return null;
                return new KakaoRepostViewModel(postData);
            }

            if (bundledFeed.type == "share" && bundledFeed.activities is { Count: > 0 })
            {
                var activity = bundledFeed.activities[0];
                activity.@object = bundledFeed.original_activity;
                return new KakaoPostViewModel(activity);
            }
        }

        return new KakaoPostViewModel(postData);
    }

    /// <summary>
    /// Loads the logged-in Kakao Story user's id into Shared.KakaoUserId so post
    /// action sheets can distinguish own posts (e.g. hide vs. delete).
    /// </summary>
    private static async Task SaveCurrentUserAsync()
    {
        try
        {
            var profile = await KakaoStoryApiHandler.GetProfileData();
            Shared.KakaoUserId = profile?.id;
        }
        catch { Shared.KakaoUserId = null; }
    }

    private static readonly DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static long ToUnixTime(DateTime date)
    {
        return Convert.ToInt64((date - epoch).TotalSeconds);
    }

    public static string GetStringFromQuoteData(List<QuoteData> datas, bool preserveQuote)
    {
        StringBuilder sb = new();
        foreach (var data in datas)
        {
            if (preserveQuote)
            {
                if (data.type.Equals("profile"))
                {
                    sb.Append("{!{" + JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }) + "}!}");
                }
                else if (data.type.Equals("emoticon"))
                {
                    sb.Append("{!{" + JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }) + "}!}");
                }
                else
                    sb.Append(data.text);
            }
            else
                sb.Append(data.text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Spaghetti code, should be refactored.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="escapeHashtag"></param>
    /// <returns></returns>
    public static List<QuoteData> GetQuoteDataFromString(string text, bool escapeHashtag = false)
    {
        text = text.Replace("\r\n", "\n");
        text = text.Replace("\r", "\n");
        string[] fragmentBases = text.Split(new string[] { "{!{" }, StringSplitOptions.None);
        var returnData = new List<QuoteData>();
        int count = 0;
        foreach (string fragmentBase in fragmentBases)
        {
            if (count % 2 == 0)
            {
                string str = fragmentBase.Contains("}!}") ? fragmentBase.Split(new string[] { "}!}" }, StringSplitOptions.None)[1] : fragmentBase;
                if (str.Contains('#') && !escapeHashtag)
                {
                    string[] rawStr = str.Split(new string[] { "#" }, StringSplitOptions.None);
                    if (rawStr[0].Length > 0)
                    {
                        returnData.Add(new QuoteData()
                        {
                            type = "text",
                            text = rawStr[0]
                        });
                    }
                    for (int i = 1; i < rawStr.Length; i++)
                    {
                        string strNow = rawStr[i];
                        int splitCounter = Math.Min(strNow.IndexOf(" "), strNow.IndexOf("\n"));
                        if (splitCounter >= 0)
                        {
							string hashTag = strNow[..splitCounter];
                            string otherStr = strNow[splitCounter..];
                            if (hashTag.Length > 0)
                            {
                                returnData.Add(new QuoteData()
                                {
                                    type = "hashtag",
                                    hashtag_type = "",
                                    hashtag_type_id = "",
                                    text = "#" + hashTag
                                });
                            }
                            else
                            {
                                returnData.Add(new QuoteData()
                                {
                                    type = "text",
                                    text = "#"
                                });
                            }
                            if (otherStr.Length > 0)
                            {
                                returnData.Add(new QuoteData()
                                {
                                    type = "text",
                                    text = otherStr
                                });
                            }
                        }
                        else
                        {
                            returnData.Add(new QuoteData()
                            {
                                type = "hashtag",
                                hashtag_type = "",
                                hashtag_type_id = "",
                                text = "#" + strNow
                            });
                        }
                    }
                }
                else
                {
                    var quoteData = new QuoteData()
                    {
                        type = "text",
                        text = str
                    };
                    returnData.Add(quoteData);
                }
                count++;
            }
            else
            {
                string[] strs = fragmentBase.Split(new string[] { "}!}" }, StringSplitOptions.None);
                string jsonStr = strs[0];
                var quoteData = JsonConvert.DeserializeObject<QuoteData>(jsonStr);
                count++;
                returnData.Add(quoteData);
                if (strs.Length == 2)
                {
                    var quoteData2 = new QuoteData()
                    {
                        type = "text",
                        text = strs[1]
                    };
                    returnData.Add(quoteData2);
                    count++;
                }
            }
        }
        return returnData;
    }

    /// <summary>
    /// Converts the editor contents (text/hashtag/profile/sticker) into Kakao Story
    /// QuoteData decorators. Profile mentions keep the bare display name (no "@")
    /// and the friend's id, matching the Kakao Story API format. Sticker contents
    /// are skipped here; they are uploaded as images separately.
    /// </summary>
    public static List<QuoteData> GetQuoteDataFromContents(List<BaseContent> contents)
    {
        if (contents == null) return [];

        var quoteDatas = new List<QuoteData>();
        foreach (var content in contents)
        {
            if (content is TextContent textContent)
            {
                if (!string.IsNullOrEmpty(textContent.Text)) quoteDatas.Add(new QuoteData { type = "text", text = textContent.Text });
            }
            else if (content is HashtagContent hashtagContent)
            {
                quoteDatas.Add(new QuoteData
                {
                    type = "hashtag",
                    hashtag_type = "",
                    hashtag_type_id = "",
                    text = "#" + hashtagContent.Tag
                });
            }
            else if (content is ProfileContent profileContent)
            {
                var friend = Shared.KakaoFriends?.FirstOrDefault(profile => profile.display_name == profileContent.Nickname);
                if (friend != null)
                {
                    quoteDatas.Add(new QuoteData
                    {
                        type = "profile",
                        id = friend.id,
                        text = profileContent.Nickname
                    });
                }
                else
                {
                    quoteDatas.Add(new QuoteData { type = "text", text = "@" + profileContent.Nickname });
                }
            }
        }
        return quoteDatas;
    }

    public static string GetTimeString(DateTime created_at, DateTime? modified_at = null)
    {
        int offset = DateTimeOffset.Now.Offset.Hours;
        string dateText = created_at.AddHours(offset).ToString();
        var diffTime = DateTime.Now.Subtract(created_at.AddHours(offset));
        if (diffTime.TotalSeconds < 60)
        {
            dateText = "방금 전";
        }
        else if (diffTime.TotalMinutes < 60)
        {
            dateText = ((int)diffTime.TotalMinutes).ToString() + "분 전";
        }
        else if (diffTime.TotalHours < 24)
        {
            dateText = ((int)diffTime.TotalHours).ToString() + "시간 전";
        }

        if (modified_at != null) dateText += " (수정됨)";
        return dateText;
    }
}
