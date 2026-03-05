using History.Commons.DataTypes.Contents;
using SuggestingBox.Maui;

namespace History.MobileClient.Helpers;

public static class MentionHelper
{
    private static readonly SuggestionFormat mentionFormat = new()
    {
        ForegroundColor = Colors.White,
        BackgroundColor = Application.Current.Resources["Primary"] as Color,
        Bold = FormatEffect.On
    };

    private static readonly SuggestionFormat stickerFormat = new()
    {
        ForegroundColor = Colors.White,
        BackgroundColor = Application.Current.Resources["Primary"] as Color,
        Bold = FormatEffect.On
    };

    public static void InsertToken(SuggestingBox.Maui.SuggestingBox suggestingBox, string prefix, string displayText, object item, SuggestionFormat format)
    {
        string currentText = suggestingBox.Text ?? string.Empty;
        var existingTokens = suggestingBox.GetTokens().ToList();

        // Append the token text at the end of the current text
        string tokenText = prefix + displayText;
        string newText = currentText + tokenText + " ";
        int tokenStartIndex = currentText.Length;

        existingTokens.Add(new SuggestingBoxTokenInfo(tokenStartIndex, prefix, displayText, format, item));
        suggestingBox.SetContent(newText, existingTokens);
    }

    public static void InsertUser(SuggestingBox.Maui.SuggestingBox suggestingBox, string userId, string nickname)
    {
        var profileContent = new ProfileContent { UserId = userId, Nickname = nickname };
        InsertToken(suggestingBox, "@", nickname, profileContent, mentionFormat);
    }

    public static void InsertSticker(SuggestingBox.Maui.SuggestingBox suggestingBox, string stickerId, string stickerContentId)
    {
        var stickerContent = new StickerContent { StickerId = stickerId, StickerContentId = stickerContentId };
        InsertToken(suggestingBox, "@", " * 스티커 * ", stickerContent, stickerFormat);
    }

    public static void AppendText(SuggestingBox.Maui.SuggestingBox suggestingBox, string text)
    {
        string currentText = suggestingBox.Text ?? string.Empty;
        var existingTokens = suggestingBox.GetTokens();
        suggestingBox.SetContent(currentText + text, existingTokens);
    }

    public static void AppendUser(SuggestingBox.Maui.SuggestingBox suggestingBox, string userId, string nickname, bool showKeyboard = false)
    {
        if (userId == Shared.UserId) return;

        var profileContent = new ProfileContent { UserId = userId, Nickname = nickname };
        string currentText = suggestingBox.Text ?? string.Empty;
        if (currentText.Length > 0 && !currentText.EndsWith(' ')) currentText += " ";

        var existingTokens = suggestingBox.GetTokens().ToList();
        string tokenText = "@" + nickname;
        int tokenStartIndex = currentText.Length;
        string newText = currentText + tokenText + " ";

        existingTokens.Add(new SuggestingBoxTokenInfo(tokenStartIndex, "@", nickname, mentionFormat, profileContent));
        suggestingBox.SetContent(newText, existingTokens);

        if (showKeyboard) suggestingBox.Focus();
    }

    public static void AppendSticker(SuggestingBox.Maui.SuggestingBox suggestingBox, string stickerId, string stickerContentId, bool showKeyboard = false)
    {
        var stickerContent = new StickerContent { StickerId = stickerId, StickerContentId = stickerContentId };
        string currentText = suggestingBox.Text ?? string.Empty;
        if (currentText.Length > 0 && !currentText.EndsWith(' ')) currentText += " ";

        var existingTokens = suggestingBox.GetTokens().ToList();
        string tokenText = "@" + " * 스티커 * ";
        int tokenStartIndex = currentText.Length;
        string newText = currentText + tokenText + " ";

        existingTokens.Add(new SuggestingBoxTokenInfo(tokenStartIndex, "@", " * 스티커 * ", stickerFormat, stickerContent));
        suggestingBox.SetContent(newText, existingTokens);

        if (showKeyboard) suggestingBox.Focus();
    }

    public static List<BaseContent> GetContents(SuggestingBox.Maui.SuggestingBox suggestingBox)
    {
        var result = new List<BaseContent>();
        string fullText = suggestingBox.Text ?? string.Empty;
        var tokens = suggestingBox.GetTokens();
        int currentIndex = 0;

        foreach (var token in tokens.OrderBy(token => token.StartIndex))
        {
            // Add plain text before this token
            if (token.StartIndex > currentIndex)
            {
                string plainText = fullText[currentIndex..token.StartIndex];
                if (!string.IsNullOrEmpty(plainText)) result.Add(new TextContent { Text = plainText });
            }

            // Add the token content based on Item type
            if (token.Item is ProfileContent profileContent)
                result.Add(profileContent);
            else if (token.Item is StickerContent stickerContent)
                result.Add(stickerContent);
            else if (token.Prefix == "#")
                result.Add(new HashtagContent { Tag = token.DisplayText });
            else
                result.Add(new TextContent { Text = token.Prefix + token.DisplayText });

            currentIndex = token.StartIndex + token.Prefix.Length + token.DisplayText.Length;
        }

        // Add remaining text after last token
        if (currentIndex < fullText.Length)
        {
            string remainingText = fullText[currentIndex..];
            if (!string.IsNullOrEmpty(remainingText)) result.Add(new TextContent { Text = remainingText });
        }

        return result;
    }

    public static List<string> GetHashtags(SuggestingBox.Maui.SuggestingBox suggestingBox) =>
        suggestingBox.GetTokens()
            .Where(token => token.Prefix == "#")
            .Select(token => token.DisplayText)
            .ToList();
}
