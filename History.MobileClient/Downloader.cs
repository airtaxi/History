using RestSharp;

namespace History.MobileClient;

public static class Downloader
{
	public delegate void DownloadProgressChangedHandler(double percentage);
	public static event DownloadProgressChangedHandler DownloadProgressChanged;
	private readonly static SemaphoreSlim s_semaphore = new(1);

	public static async Task DownloadFileAsync(string requestUri, string destinationPath)
	{
		await s_semaphore.WaitAsync();
		using var httpClient = new HttpClient();
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
		using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;
		var totalReadBytes = 0L;
		var buffer = new byte[8192];

		int bytesRead;
		using var fileStream = File.Create(destinationPath);
		using var stream = await response.Content.ReadAsStreamAsync();
		do
		{
			bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			await fileStream.WriteAsync(buffer, 0, bytesRead);

			totalReadBytes += bytesRead;
			TriggerProgressChanged(totalReadBytes, totalBytes);

		} while (bytesRead > 0);
		ClearDownloadProgressChangedEventHandler();
		s_semaphore.Release();
	}

	private static void TriggerProgressChanged(long totalReadBytes, long totalBytes)
	{
		if (DownloadProgressChanged != null && totalBytes != -1)
		{
			var percentage = (double)totalReadBytes / totalBytes * 100;
			DownloadProgressChanged(percentage);
		}
	}


	private static void ClearDownloadProgressChangedEventHandler() => DownloadProgressChanged = null;

	public static async Task<string> DownloadString(string url)
	{
		var client = new RestClient(url);
		var response = await client.ExecuteGetAsync(new RestRequest());
		return response.Content;
	}
}	
