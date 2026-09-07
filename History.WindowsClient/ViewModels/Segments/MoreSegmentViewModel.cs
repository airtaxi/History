namespace History.WindowsClient.ViewModels.Segments;

// Marks the point where the body text was truncated; rendered as a trailing
// " ... 더보기" indicator in the body content control.
public sealed record MoreSegmentViewModel : BodyContentSegmentViewModel;