using History.Commons.Api.KakaoStory;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using Newtonsoft.Json;
using System.Text;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.Commons.KakaoStory;

public partial class CommonKakaoStoryUtils
{
    // Guide shown when entering the Kakao Story-only write mode and when blocking a
    // post without any user mention. Kakao Story-only writing mirrors the post to
    // History after the Kakao Story upload succeeds; Kakao Story mentions are
    // resolved to History users by nickname.
    public const string KakaoOnlyWriteGuideMessage = "이 모드에서 작성한 게시글은 카카오스토리에 게시된 후, 동일한 내용이 히스토리에도 게시됩니다. 카카오스토리 친구를 언급한 경우 히스토리 게시글에는 닉네임이 동일한 히스토리 친구로 언급됩니다. 카카오스토리에 게시하지 않고 히스토리에만 게시하려면 히스토리탭에서 게시글을 작성해주세요";

    /// <summary>
    /// Uploads the current Kakao Story KAuth id token to the server so the
    /// server-side polling service can deliver Kakao Story notifications via FCM.
    /// The notification filter flags mirror the settings page. Skipped when the
    /// master notification toggle is off (the server session is deleted instead).
    /// Failures are swallowed: polling is a best-effort convenience feature.
    /// </summary>
    public static async Task UploadTokenToServerAsync()
    {
        try
        {
            if ((Configuration.GetValue<bool?>("KakaoStoryNotificationEnabled") ?? true) == false) return;
            var idToken = await KakaoStoryApiHandler.EnsureKAuthTokenAsync();
            if (idToken == null) return;

            var requestDto = new UpdateKakaoStoryTokenRequestDto
            {
                IdToken = idToken,
                IsFavoriteFriendNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryFavoriteFriendNotificationEnabled") ?? true,
                IsEmotionNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryEmotionNotificationEnabled") ?? true
            };
            await CommonShared.ApiHandler.TryExecuteRequestAsync(new UpdateKakaoStoryToken(requestDto));
        }
        catch { }
    }

    /// <summary>
    /// Removes the user's Kakao Story session from the server polling service.
    /// </summary>
    public static async Task DeleteTokenFromServerAsync()
    {
        try { await CommonShared.ApiHandler.TryExecuteRequestAsync(new DeleteKakaoStoryToken()); }
        catch { }
    }

    /// <summary>
    /// Reloads the friends list and the current user id. Used when the caches are
    /// empty (cold start) so mention surfaces and post action sheets stay accurate.
    /// Failures leave the caches untouched: the next caller retries the refresh.
    /// </summary>
    protected static async Task RefreshSessionCachesAsync()
    {
        try
        {
            CommonShared.KakaoFriends = (await KakaoStoryApiHandler.GetFriends())?.profiles;
            await SaveCurrentUserAsync();
        }
        catch { }
        _ = KakaoStoryApiHandler.EnsureEmoticonCredentialAsync(); // Warm up so first emoticons render immediately.
    }

    /// <summary>
    /// Feed verbs that are not a user's post and have no single-post surface to render:
    /// - "suggest"    -> Kakao Story's own recommendation card (오늘의 추천 친구).
    /// - "aggregated" -> timehop-style bundles carrying several posts in object.objects.
    /// Add to this set as new ones turn up.
    ///
    /// This is deliberately a blocklist rather than an allowlist of renderable verbs.
    /// Kakao Story's verb vocabulary is undocumented, and a verb other than "post" can
    /// still be a real post (@object is "the shared post, for a share activity"), so an
    /// allowlist would fail closed and silently drop content with nothing on screen to
    /// say so. A blocklist fails open: an unknown card is visible and is one line to fix.
    /// </summary>
    protected static readonly HashSet<string> NonPostVerbs = ["suggest", "aggregated"];


    /// <summary>
    /// Loads the logged-in Kakao Story user's id into CommonShared.KakaoUserId so post
    /// action sheets can distinguish own posts (e.g. hide vs. delete).
    /// </summary>
    protected static async Task SaveCurrentUserAsync()
    {
        try
        {
            var profile = await KakaoStoryApiHandler.GetProfileData();
            CommonShared.KakaoUserId = profile?.id;
        }
        catch { CommonShared.KakaoUserId = null; }
    }

    protected static readonly DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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
            else if (content is HyperlinkContent hyperlinkContent)
            {
                if (!string.IsNullOrEmpty(hyperlinkContent.Url)) quoteDatas.Add(new QuoteData { type = "text", text = hyperlinkContent.Url });
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
                // Resolve by the exact user id first; a nickname-only match is unreliable.
                var friend = CommonShared.KakaoFriends?.FirstOrDefault(profile => profile.id == profileContent.UserId) ?? CommonShared.KakaoFriends?.FirstOrDefault(profile => profile.display_name == profileContent.Nickname);
                if (friend != null)
                {
                    quoteDatas.Add(new QuoteData
                    {
                        type = "profile",
                        id = friend.id,
                        text = friend.display_name ?? profileContent.Nickname
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

    /// <summary>
    /// Converts Kakao Story QuoteData decorators (text/hashtag/profile/emoticon/image)
    /// into BaseContent so shared rendering/edit surfaces (e.g. PostImageRendererHelper)
    /// can consume Kakao Story posts. The emoticon is preserved as a "(이모티콘)"
    /// placeholder text token so editing keeps it instead of dropping it entirely;
    /// when preserveEmoticon is true it becomes a StickerContent carrying the
    /// Referer-signed emoticon URL (image rendering only), falling back to the
    /// placeholder text when the credential is not available.
    /// </summary>
    public static List<BaseContent> ConvertToBaseContents(List<QuoteData> quoteDatas, bool preserveEmoticon = false)
    {
        var contents = new List<BaseContent>();
        var mediaContents = new List<BaseContent>();
        foreach (var data in quoteDatas)
        {
            switch (data.type)
            {
                case "text":
                    contents.Add(new TextContent { Text = data.text });
                    break;
                case "hashtag":
                    contents.Add(new HashtagContent { Tag = data.text.TrimStart('#') });
                    break;
                case "profile":
                    contents.Add(new ProfileContent { UserId = data.id, Nickname = data.text });
                    break;
                case "image":
                    // Appended after all text fragments to mirror the UI layout
                    // (FormattedText first, media carousel last).
                    var mediaUrl = data.media?.url ?? data.media?.thumbnail_url;
                    if (mediaUrl != null) mediaContents.Add(new MediaContent { MediaId = mediaUrl, MimeType = "image/jpeg" });
                    else mediaContents.Add(new TextContent { Text = "(이미지)" });
                    break;
                case "emoticon":
                    if (preserveEmoticon)
                    {
                        var emoticonUrl = KakaoStoryApiHandler.GetEmoticonUrlSync(data.item_id, data.resource_id.ToString());
                        if (emoticonUrl != null) contents.Add(new StickerContent { StickerMediaId = emoticonUrl, IsAnimated = false });
                        else contents.Add(new TextContent { Text = "(이모티콘)" });
                    }
                    else contents.Add(new TextContent { Text = "(이모티콘)" });
                    break;
            }
        }
        contents.AddRange(mediaContents);
        return contents;
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
