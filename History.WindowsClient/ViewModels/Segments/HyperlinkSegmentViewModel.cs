namespace History.WindowsClient.ViewModels.Segments;

// DisplayText carries the (possibly trimmed) rendered text while Url keeps the full open target.
public sealed record HyperlinkSegmentViewModel(string Url, string DisplayText) : BodyContentSegmentViewModel;