using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public class TextTypeContentsViewModel(List<BaseContent> textTypeContents, PostType postType, bool hasMedias) : IContentViewModel
{
    public List<BaseContent> TextTypeContents { get; } = textTypeContents;
    public FormattedString FormattedString { get; set; } = Utils.GenerateFormattedStringFromTextTypeContents(textTypeContents, postType, hasMedias);

    // Kakao Story overload: renders QuoteData (text/hashtag/profile/emoticon) into the same surface.
    // Text/hashtag/profile are also converted into BaseContent so long-press copy works.
    public TextTypeContentsViewModel(List<QuoteData> quoteDatas, PostType postType) : this(ConvertToBaseContents(quoteDatas), postType, false) => FormattedString = Utils.GenerateFormattedStringFromQuoteData(quoteDatas, postType);

    public static List<BaseContent> ConvertToBaseContents(List<QuoteData> quoteDatas)
    {
        var contents = new List<BaseContent>();
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
                case "emoticon":
                    // Preserve the emoticon as a placeholder token so editing a Kakao
                    // Story post keeps it instead of dropping it entirely.
                    contents.Add(new TextContent { Text = "(이모티콘)" });
                    break;
            }
        }
        return contents;
    }
}
