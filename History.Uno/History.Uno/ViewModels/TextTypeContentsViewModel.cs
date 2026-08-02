using History.Commons.DataTypes.Contents;
using History.Uno.Enums;

namespace History.Uno.ViewModels;

public class TextTypeContentsViewModel(List<BaseContent> textTypeContents, PostType postType, bool hasMedias) : IContentViewModel
{
    public List<BaseContent> TextTypeContents { get; } = textTypeContents;
    public List<TextContentRun> Runs { get; } = Utils.GenerateTextContentRuns(textTypeContents, postType, hasMedias);
}
