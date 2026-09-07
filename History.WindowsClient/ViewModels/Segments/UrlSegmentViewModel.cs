namespace History.WindowsClient.ViewModels.Segments;

// A URL detected inside TextContent; rendered and opened the same way as HyperlinkContent.
// DisplayText carries the (possibly trimmed) rendered text while Url keeps the full open target.
public sealed record UrlSegmentViewModel(string Url, string DisplayText) : BodyContentSegmentViewModel;