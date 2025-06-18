using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public class TextAndProfileContentsViewModel(List<BaseContent> textAndProfileContents, PostType postType, bool hasMedias) : IContentViewModel
{
    public FormattedString FormattedString { get; set; } = Utils.GenerateSpanFromTextAndProfileContents(textAndProfileContents, postType, hasMedias);
}
