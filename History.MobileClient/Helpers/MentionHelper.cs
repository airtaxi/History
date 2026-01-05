using History.Commons.DataTypes.Contents;
using SpeakLink.Mention;

namespace History.MobileClient.Helpers;

public static class MentionHelper
{
    public static readonly Dictionary<int, string> MentionIdMap = [];

    public static void InsertUser(MentionEditor mentionEditor, string userId, string nickname)
    {
        var userMapId = GetUserMapId(userId);

        if (!MentionIdMap.Any(x => GetUserMapId(x.Value) == userId)) MentionIdMap[MentionIdMap.Count] = userMapId;
        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == userMapId).Key.ToString(), nickname + ' ');
    }

    public static void InsertSticker(MentionEditor mentionEditor, string stickerId, string stickerContentId)
    {
        var stickerMapId = GetStickerMapId(stickerId) + "_" + stickerContentId;

        AppendText(mentionEditor, " @");

        if (!MentionIdMap.Any(x => x.Value == stickerId)) MentionIdMap[MentionIdMap.Count] = stickerMapId;
        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == stickerMapId).Key.ToString(), " * " + "스티커" + " * ");
    }

    private static string GetStickerMapId(string stickerId) => "!_s" + stickerId;
    private static string GetUserMapId(string userId) => "!_u" + userId;

    public static void AppendText(MentionEditor mentionEditor, string text, bool showKeyboard = false)
    {
        var formattedText = mentionEditor.FormattedText?.Spans?.ToList() ?? [];
        formattedText.Add(new Span() { Text = text });

        var result = new FormattedString();
        formattedText.ForEach(result.Spans.Add);
        mentionEditor.SendFormattedTextChanged(result);

        MoveCursorToEnd(mentionEditor, showKeyboard);
    }

    public static void AppendUser(MentionEditor mentionEditor, string userId, string nickname, bool showKeyboard = false)
    {
        if (userId == Shared.UserId) return;

        var userMapId = GetUserMapId(userId);

        if (!MentionIdMap.Any(x => x.Value == userId)) MentionIdMap[MentionIdMap.Count] = userMapId;

        // Add " @" to the end of the text to allow InsertMention to work properly
        var formattedText = mentionEditor.FormattedText?.Spans?.ToList() ?? [];
        mentionEditor.Text ??= string.Empty;
        mentionEditor.Text += " @";
        mentionEditor.CursorPosition = mentionEditor.Text.Length;
        mentionEditor.SelectionLength = 0;

        // Call InsertMention to insert the mention span
        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == userMapId).Key.ToString(), nickname + ' ');

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

    public static void AppendSticker(MentionEditor mentionEditor, string stickerId, string stickerContentId, bool showKeyboard = false)
    {
        var stickerMapId = GetStickerMapId(stickerId) + "_" + stickerContentId;

        if (!MentionIdMap.Any(x => x.Value == stickerMapId)) MentionIdMap[MentionIdMap.Count] = stickerMapId;

        // Add " " to the end of the text to allow InsertMention to work properly
        var formattedText = mentionEditor.FormattedText?.Spans?.ToList() ?? [];
        mentionEditor.Text ??= string.Empty;
        mentionEditor.Text += " @";
        mentionEditor.CursorPosition = mentionEditor.Text.Length;
        mentionEditor.SelectionLength = 0;

        // Call InsertMention to insert the mention span
        mentionEditor.InsertMention(MentionIdMap.FirstOrDefault(x => x.Value == stickerMapId).Key.ToString(), " * " + "스티커" + " * ");

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

    public static bool IsUser(int index)
    {
        var mentionMapId = MentionIdMap.GetValueOrDefault(index);
        return mentionMapId != null && mentionMapId.StartsWith("!_u");
    }

    public static ProfileContent GetProfileContent(int index)
    {
        var userMapId = MentionIdMap.GetValueOrDefault(index);
        return userMapId != null && userMapId.StartsWith("!_u") ? new ProfileContent() { UserId = userMapId[3..] } : null;
    }

    public static StickerContent GetStickerContent(int index)
    {
        var stickerMapId = MentionIdMap.GetValueOrDefault(index);
        if (stickerMapId != null && stickerMapId.StartsWith("!_s"))
        {
            var parts = stickerMapId[3..].Split('_');
            if (parts.Length == 2)
            {
                return new() { StickerId = parts[0], StickerContentId = parts[1] };
            }
        }
        return null;
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
