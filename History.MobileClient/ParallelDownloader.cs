namespace History.MobileClient;

public static class ParallelDownloader
{
    public static async Task DownloadFilesAsync(IEnumerable<(string Uri, string FilePath)> items)
    {
        var tasks = items.Select(item => DownloadFileAsync(item.Uri, item.FilePath));
        await Task.WhenAll(tasks);
    }

    private static async Task DownloadFileAsync(string requestUri, string destinationPath)
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(destinationPath);
        await response.Content.CopyToAsync(fileStream);
    }
}
