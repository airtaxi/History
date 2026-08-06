using History.Commons.DataTypes.Contents;
using SuggestingBox.Maui;

namespace History.MobileClient.Helpers;

public static class MentionHelper
{
    private const double StickerImageWidthRequest = 80;

    private static readonly HttpClient s_httpClient = new();
    private static readonly SemaphoreSlim s_stickerImageDataCacheSemaphore = new(1, 1);
    private static readonly Dictionary<string, byte[]> s_stickerImageDataCache = [];

    private static readonly SuggestionFormat s_mentionFormat = new()
    {
        ForegroundColor = Colors.White,
        BackgroundColor = Application.Current.Resources["Primary"] as Color,
        Bold = FormatEffect.On
    };

    private static readonly SuggestionFormat s_stickerFallbackFormat = new()
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
        InsertToken(suggestingBox, "@", nickname, profileContent, s_mentionFormat);
    }

    public static async Task<bool> InsertStickerAsync(SuggestingBox.Maui.SuggestingBox suggestingBox, StickerContent stickerContent)
    {
        var token = await CreateStickerImageTokenAsync(0, stickerContent);
        if (token == null) return false;

        string currentText = suggestingBox.Text ?? string.Empty;
        var existingTokens = suggestingBox.GetTokens().ToList();
        if (currentText.Length > 0 && !currentText.EndsWith('\n')) currentText += "\n";

        token.StartIndex = currentText.Length;
        existingTokens.Add(token);
        suggestingBox.SetContent(currentText + SuggestingBoxText.ImagePlaceholderString + "\n", existingTokens);
        return true;
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

        existingTokens.Add(new SuggestingBoxTokenInfo(tokenStartIndex, "@", nickname, s_mentionFormat, profileContent));
        suggestingBox.SetContent(newText, existingTokens);

        if (showKeyboard) suggestingBox.Focus();
    }

    public static async Task<bool> AppendStickerAsync(SuggestingBox.Maui.SuggestingBox suggestingBox, StickerContent stickerContent, bool showKeyboard = false)
    {
        var inserted = await InsertStickerAsync(suggestingBox, stickerContent);

        if (showKeyboard) suggestingBox.Focus();
        return inserted;
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
            int tokenStartIndex = Math.Clamp(token.StartIndex, currentIndex, fullText.Length);
            if (tokenStartIndex > currentIndex)
            {
                string plainText = fullText[currentIndex..tokenStartIndex];
                if (!string.IsNullOrEmpty(plainText)) result.Add(new TextContent { Text = plainText });
            }

            // Add the token content based on Item type
            if (token.Item is ProfileContent profileContent) result.Add(profileContent);
            else if (token.Item is StickerContent stickerContent) result.Add(stickerContent);
            else if (token.Kind == SuggestingBoxTokenKind.Mention && token.Prefix == "#") result.Add(new HashtagContent { Tag = token.DisplayText });
            else if (token.Kind == SuggestingBoxTokenKind.Mention) result.Add(new TextContent { Text = token.Prefix + token.DisplayText });

            currentIndex = Math.Clamp(token.EndIndex, currentIndex, fullText.Length);
        }

        // Add remaining text after last token
        if (currentIndex < fullText.Length)
        {
            string remainingText = fullText[currentIndex..];
            if (!string.IsNullOrEmpty(remainingText)) result.Add(new TextContent { Text = remainingText });
        }

        return result;
    }

    public static List<string> GetHashtags(SuggestingBox.Maui.SuggestingBox suggestingBox) => [.. suggestingBox.GetTokens()
        .Where(token => token.Kind == SuggestingBoxTokenKind.Mention && token.Prefix == "#")
        .Select(token => token.DisplayText)];

    public static async Task<SuggestingBoxTokenInfo> CreateStickerImageTokenAsync(int startIndex, StickerContent stickerContent)
    {
        if (stickerContent == null) return null;

        var imageData = await GetStickerImageDataAsync(stickerContent.StickerMediaId);
        if (imageData.Length == 0) return null;

        return SuggestingBoxTokenInfo.CreateImage(startIndex, imageData, alternativeText: "스티커", widthRequest: StickerImageWidthRequest, item: stickerContent, tag: stickerContent.StickerContentId);
    }

    public static SuggestingBoxTokenInfo CreateStickerFallbackToken(int startIndex, StickerContent stickerContent) =>
        new(startIndex, " * ", "스티커 * ", s_stickerFallbackFormat, stickerContent);

    public static async Task<byte[]> GetStickerImageDataAsync(string stickerMediaId)
    {
        if (string.IsNullOrEmpty(stickerMediaId)) return [];

        await s_stickerImageDataCacheSemaphore.WaitAsync();
        try
        {
            if (s_stickerImageDataCache.TryGetValue(stickerMediaId, out var cachedImageData)) return cachedImageData;
        }
        finally { s_stickerImageDataCacheSemaphore.Release(); }

        try
        {
            var mediaUri = Utils.GenerateMediaUri(stickerMediaId);
            if (mediaUri == null) return [];

            var imageData = await s_httpClient.GetByteArrayAsync(mediaUri);

            await s_stickerImageDataCacheSemaphore.WaitAsync();
            try { s_stickerImageDataCache[stickerMediaId] = imageData; }
            finally { s_stickerImageDataCacheSemaphore.Release(); }

            return imageData;
        }
        catch
        {
            return [];
        }
    }
}
