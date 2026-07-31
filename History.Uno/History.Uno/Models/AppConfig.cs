namespace History.Uno.Models;

public record AppConfig
{
    public string Environment { get; init; }
    public string ApiEndpoint { get; init; }
}