namespace History.WindowsClient.ViewModels.Segments;

// Carries the navigation host used by the profile span tap.
public sealed record ProfileSegmentViewModel(string UserId, string Nickname, BaseViewModel BaseViewModel) : BodyContentSegmentViewModel;