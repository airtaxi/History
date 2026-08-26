using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Models;

public sealed partial class ApplicationSettings : ObservableObject
{
    [ObservableProperty]
    public partial ElementTheme Theme { get; set; } = ElementTheme.Default;

    [ObservableProperty]
    public partial bool IsAutomaticUpdateCheckEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string AccessToken { get; set; }

    [ObservableProperty]
    public partial string RefreshToken { get; set; }
}