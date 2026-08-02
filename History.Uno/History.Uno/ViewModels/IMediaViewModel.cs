using Microsoft.UI.Xaml.Media;

namespace History.Uno.ViewModels;

public interface IMediaViewModel
{
    string Uri { get; set; }
    Stretch Stretch { get; set; }
    double MaxWidth { get; set; }
}
