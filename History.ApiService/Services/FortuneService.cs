using System.Text.Json;
using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class FortuneService(IMongoDatabase database, ILogger<FortuneService> logger) : IFortuneService
{
    private static readonly char[] s_koreanInitials =
    [
        'ㄱ', 'ㄴ', 'ㄷ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅅ', 'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    ];

    private static readonly int[] s_scoreWeights = [5, 12, 28, 40, 15];
    private static readonly Lock s_fortuneDataLock = new();
    private static Dictionary<int, List<FortuneItem>> s_wealthFortunes;
    private static Dictionary<int, List<FortuneItem>> s_successFortunes;
    private static Dictionary<int, List<FortuneItem>> s_loveFortunes;

    private readonly IMongoCollection<DailyFortuneRecord> _dailyFortuneRecords = database.GetCollection<DailyFortuneRecord>("DailyFortuneRecords");

    /// <inheritdoc/>
    public async Task<bool> HasDrawnTodayAsync(string userId)
    {
        var today = GetKstDateString();
        var filter = Builders<DailyFortuneRecord>.Filter.Eq(record => record.UserId, userId) &
                     Builders<DailyFortuneRecord>.Filter.Eq(record => record.Date, today);
        return await _dailyFortuneRecords.Find(filter).AnyAsync();
    }

    /// <inheritdoc/>
    public async Task RecordDrawAsync(string userId)
    {
        var today = GetKstDateString();
        var record = new DailyFortuneRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Date = today
        };

        try { await _dailyFortuneRecords.InsertOneAsync(record); }
        catch (MongoWriteException) { }
    }

    /// <inheritdoc/>
    public string CreateFortuneMessage(string nickname)
    {
        LoadFortuneData();

        if (s_wealthFortunes is not { Count: > 0 } || s_successFortunes is not { Count: > 0 } || s_loveFortunes is not { Count: > 0 }) return string.Empty;

        var wealth = PickFortune(s_wealthFortunes);
        var success = PickFortune(s_successFortunes);
        var love = PickFortune(s_loveFortunes);
        var noblePersonInitial1 = s_koreanInitials[Random.Shared.Next(s_koreanInitials.Length)];
        var noblePersonInitial2 = s_koreanInitials[Random.Shared.Next(s_koreanInitials.Length)];

        return $"{nickname}님의 오늘의 운세:\n\n" +
            $"재물운 {FormatStars(wealth.Score)}\n{wealth.Text}\n\n" +
            $"성공운 {FormatStars(success.Score)}\n{success.Text}\n\n" +
            $"애정운 {FormatStars(love.Score)}\n{love.Text}\n\n" +
            $"오늘의 귀인: {noblePersonInitial1}{noblePersonInitial2}\n";
    }

    private static string GetKstDateString() => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9)).ToString("yyyy-MM-dd");

    private void LoadFortuneData()
    {
        if (s_wealthFortunes != null) return;

        lock (s_fortuneDataLock)
        {
            if (s_wealthFortunes != null) return;

            try
            {
                var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                s_wealthFortunes = LoadFortuneFile(Path.Combine(assetsPath, "FortuneWealth.json"), options);
                s_successFortunes = LoadFortuneFile(Path.Combine(assetsPath, "FortuneSuccess.json"), options);
                s_loveFortunes = LoadFortuneFile(Path.Combine(assetsPath, "FortuneLove.json"), options);

                logger.LogInformation("[FORTUNE] Loaded fortunes: Wealth={Wealth}, Success={Success}, Love={Love}", s_wealthFortunes.Values.Sum(list => list.Count), s_successFortunes.Values.Sum(list => list.Count), s_loveFortunes.Values.Sum(list => list.Count));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "[FORTUNE] Error loading fortune data");
                s_wealthFortunes ??= [];
                s_successFortunes ??= [];
                s_loveFortunes ??= [];
            }
        }
    }

    private static Dictionary<int, List<FortuneItem>> LoadFortuneFile(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path)) return [];

        var json = File.ReadAllText(path);
        var items = JsonSerializer.Deserialize<List<FortuneItem>>(json, options) ?? [];
        return items.GroupBy(item => item.Score).ToDictionary(group => group.Key, group => group.ToList());
    }

    private static string FormatStars(int score) => new string('★', score) + new string('☆', 5 - score);

    private static int PickWeightedScore()
    {
        var roll = Random.Shared.Next(s_scoreWeights.Sum());
        var cumulative = 0;
        for (var index = 0; index < s_scoreWeights.Length; index++)
        {
            cumulative += s_scoreWeights[index];
            if (roll < cumulative) return index + 1;
        }

        return 3;
    }

    private static FortuneItem PickFortune(Dictionary<int, List<FortuneItem>> fortunes)
    {
        var score = PickWeightedScore();
        if (fortunes.TryGetValue(score, out var items) && items.Count > 0) return items[Random.Shared.Next(items.Count)];

        var allItems = fortunes.Values.SelectMany(list => list).ToList();
        return allItems[Random.Shared.Next(allItems.Count)];
    }
}