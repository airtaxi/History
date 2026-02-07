namespace History.MobileClient.Helpers;

/// <summary>
/// Loads profanity words from filter_ko_kr.txt and detects them in text.
/// </summary>
public static class ProfanityFilterHelper
{
    private static HashSet<string> _profanityWords;

    /// <summary>
    /// Loads the profanity word list from the embedded resource file.
    /// </summary>
    public static async Task LoadAsync()
    {
        if (_profanityWords != null) return;

        _profanityWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var stream = await FileSystem.OpenAppPackageFileAsync("filter_ko_kr.txt");
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync() is { } line)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed)) _profanityWords.Add(trimmed);
        }
    }

    /// <summary>
    /// Finds all profanity words contained in the given text.
    /// Returns a distinct list of matched words.
    /// </summary>
    public static List<string> FindProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _profanityWords == null || _profanityWords.Count == 0)
            return [];

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in _profanityWords)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
                found.Add(word);
        }

        return [.. found];
    }
}
