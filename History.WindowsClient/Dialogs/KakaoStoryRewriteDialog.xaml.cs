using History.Commons.DataTypes.Contents;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.Dialogs;

// Profanity review dialog: hosts the content editor so the user can rewrite the Kakao Story
// text before it is uploaded. The edited contents are returned through EditedContents.
public sealed partial class KakaoStoryRewriteDialog : ContentDialog
{
    private const int KakaoStoryTextLimit = 4000;

    private readonly List<BaseContent> _initialContents;
    private bool _isInitialContentsLoaded;

    public KakaoStoryRewriteDialog(BaseViewModel baseViewModel, string profanityWordList, List<BaseContent> initialContents)
    {
        _initialContents = initialContents;
        InitializeComponent();

        WarningTextBlock.Text = $"카카오스토리에 게시할 글에서 다음 욕설이 감지되었습니다:\n\n{profanityWordList}\n\n글을 수정한 뒤 다시 시도해 주세요.";
        RewriteEditor.Initialize(baseViewModel);
        RewriteEditor.IsKakaoMentionMode = true;
    }

    public List<BaseContent> EditedContents { get; private set; }

    // Prefills the editor with the current Kakao Story text once the editor is loaded so the
    // document is ready; runs once even if the control is reloaded.
    private async void OnRewriteEditorLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialContentsLoaded) return;
        _isInitialContentsLoaded = true;

        await RewriteEditor.SetContentsAsync(_initialContents);
        RewriteEditor.FocusEditor();
    }

    // Blocks closing on invalid input so the empty/length errors stay visible inline; the upload
    // only receives the edited contents when the rewrite passes validation.
    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var editedContents = RewriteEditor.GetContents();
        if (editedContents.Count == 0)
        {
            ShowValidationMessage("빈 내용의 글은 작성할 수 없습니다.");
            args.Cancel = true;
            return;
        }

        var editedTextLength = RewriteEditor.Text?.Length ?? 0;
        if (editedTextLength > KakaoStoryTextLimit)
        {
            ShowValidationMessage($"카카오스토리의 글자 수 제한은 4,000자입니다. 현재 {editedTextLength}자로 제한을 초과합니다.");
            args.Cancel = true;
            return;
        }

        EditedContents = editedContents;
    }

    private void ShowValidationMessage(string message)
    {
        ValidationTextBlock.Text = message;
        ValidationTextBlock.Visibility = Visibility.Visible;
    }
}
