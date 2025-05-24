using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public class TextAndProfileContentsViewModel(List<BaseContent> textAndProfileContents, bool isTimeline, bool hasMedias) : IContentViewModel
{
    public FormattedString FormattedString { get; set; } = Utils.GenerateSpanFromTextAndProfileContents(textAndProfileContents, isTimeline, hasMedias);
}
