using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.ViewModels;

public class TextTypeContentsViewModel(List<BaseContent> textTypeContents, PostType postType, bool hasMedias) : IContentViewModel
{
    public List<BaseContent> TextTypeContents { get; } = textTypeContents;
    public FormattedString FormattedString { get; set; } = Utils.GenerateFormattedStringFromTextTypeContents(textTypeContents, postType, hasMedias);

    // Kakao Story overload: renders QuoteData (text/hashtag/profile/emoticon) into the same surface.
    // Text/hashtag/profile are also converted into BaseContent so long-press copy works.
    public TextTypeContentsViewModel(List<QuoteData> quoteDatas, PostType postType) : this(KakaoStoryUtils.ConvertToBaseContents(quoteDatas), postType, false) => FormattedString = Utils.GenerateFormattedStringFromQuoteData(quoteDatas, postType);
}
