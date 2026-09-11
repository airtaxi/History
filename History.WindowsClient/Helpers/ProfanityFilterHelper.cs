namespace History.WindowsClient.Helpers;

// Loads the profanity word list bundled with the app and detects matches in the text that is
// about to be uploaded to Kakao Story.
public static class ProfanityFilterHelper
{
    private const string ProfanityListRelativePath = "Assets/App/filter_ko_kr.txt";

    private static HashSet<string> s_profanityWords;

    public static async Task LoadAsync()
    {
        if (s_profanityWords != null) return;

        var profanityWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var profanityListPath = Path.Combine(AppContext.BaseDirectory, ProfanityListRelativePath);
        if (File.Exists(profanityListPath))
        {
            foreach (var line in await File.ReadAllLinesAsync(profanityListPath))
            {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine)) profanityWords.Add(trimmedLine);
            }
        }

        s_profanityWords = profanityWords;
    }

    public static List<string> FindProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || s_profanityWords == null || s_profanityWords.Count == 0) return [];

        var foundWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var profanityWord in s_profanityWords)
        {
            if (text.Contains(profanityWord, StringComparison.OrdinalIgnoreCase))
            {
                foundWords.Add(profanityWord);
            }
        }

        return [.. foundWords];
    }
}
