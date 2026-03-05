using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public class TextTypeContentsViewModel(List<BaseContent> textTypeContents, PostType postType, bool hasMedias) : IContentViewModel
{
    public List<BaseContent> TextTypeContents { get; } = textTypeContents;
    public FormattedString FormattedString { get; set; } = Utils.GenerateSpanFromTextTypeContents(textTypeContents, postType, hasMedias);
}
