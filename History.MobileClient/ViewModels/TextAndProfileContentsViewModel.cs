using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public class TextAndProfileContentsViewModel(List<BaseContent> textAndProfileContents) : IContentViewModel
{
    public FormattedString FormattedString { get; set; } = Utils.GenerateSpanFromTextAndProfileContents(textAndProfileContents);
}
