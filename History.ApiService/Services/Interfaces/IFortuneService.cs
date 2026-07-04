namespace History.ApiService.Services.Interfaces;

public interface IFortuneService
{
    /// <summary>
    /// Checks whether the user has already drawn a fortune today.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing true if the user already drew today.</returns>
    public Task<bool> HasDrawnTodayAsync(string userId);

    /// <summary>
    /// Records that the user has drawn a fortune today.
    /// </summary>
    /// <param name="userId">The id of the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RecordDrawAsync(string userId);

    /// <summary>
    /// Creates a fortune message for the given nickname.
    /// </summary>
    /// <param name="nickname">The nickname to address the fortune to.</param>
    /// <returns>The assembled fortune message, or null if fortune data could not be loaded.</returns>
    public string CreateFortuneMessage(string nickname);
}