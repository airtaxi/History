namespace History.WindowsClient.ViewModels.Segments;

// A URL detected inside TextContent; rendered and opened the same way as HyperlinkContent.
public sealed record UrlSegmentViewModel(string Url) : BodyContentSegmentViewModel;
