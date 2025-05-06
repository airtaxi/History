using SpeakLink.Mention;

namespace History.MobileClient.Helpers;

public static class MentionHelper
{
    public static readonly Dictionary<int, string> MentionIdMap = [];

    public static void InsertMention(MentionEditor mentionEditor, string userId, string nickname)
    {
        if (!MentionIdMap.Any(x => x.Value == userId)) MentionIdMap[MentionIdMap.Count] = userId;

        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == userId).Key.ToString(), nickname + ' ');
    }

    public static void AppendText(MentionEditor mentionEditor, string text, bool showKeyboard = false)
    {
        var formattedText = mentionEditor.FormattedText.Spans.ToList();
        formattedText.Add(new Span() { Text = text });

        var result = new FormattedString();
        formattedText.ForEach(result.Spans.Add);
        mentionEditor.SendFormattedTextChanged(result);

        MoveCursorToEnd(mentionEditor, showKeyboard);
    }

    public static void AppendMention(MentionEditor mentionEditor, string userId, string nickname, bool showKeyboard = false)
    {
        if (userId == Shared.UserId) return;

        if (!MentionIdMap.Any(x => x.Value == userId)) MentionIdMap[MentionIdMap.Count] = userId;

        // Add " @" to the end of the text to allow InsertMention to work properly
        var formattedText = mentionEditor.FormattedText.Spans.ToList();
        mentionEditor.Text += " @";
        mentionEditor.CursorPosition = mentionEditor.Text.Length;
        mentionEditor.SelectionLength = 0;

        // Call InsertMention to insert the mention span
        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == userId).Key.ToString(), nickname + ' ');

        // Insert newly added mention span to previous formatted text
        var newFormattedText = mentionEditor.FormattedText;
        var newSpan = newFormattedText.Spans.LastOrDefault();
        formattedText.Add(newSpan);

        // Update the formatted text with the new mention span
        var result = new FormattedString();
        formattedText.ForEach(result.Spans.Add);
        mentionEditor.SendFormattedTextChanged(result);

        MoveCursorToEnd(mentionEditor, showKeyboard);
    }

    private static void MoveCursorToEnd(MentionEditor mentionEditor, bool showKeyboard)
    {
        // Focus to show the keyboard
        if (showKeyboard) mentionEditor.Focus();

        // Set the cursor position to the end of the text
        var handler = mentionEditor.Handler;
#if ANDROID
        var editText = handler.PlatformView as AndroidX.AppCompat.Widget.AppCompatEditText;
        editText?.SetSelection(editText.Text.Length);
        if (showKeyboard)
        {
            var imm = Platform.AppContext.GetSystemService(Android.Content.Context.InputMethodService) as Android.Views.InputMethods.InputMethodManager;
            imm.ShowSoftInput(editText, Android.Views.InputMethods.ShowFlags.Forced);
        }
#elif IOS
            if (handler.PlatformView is UIKit.UITextView nativeView)
            {
                nativeView.SelectedRange = new Foundation.NSRange(nativeView.Text.Length, 0);
                if (showKeyboard) nativeView.BecomeFirstResponder();
            }
#endif
    }
}
