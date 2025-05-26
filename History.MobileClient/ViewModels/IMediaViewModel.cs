namespace History.MobileClient.ViewModels;

public interface IMediaViewModel
{
    public bool FullScreenSwipeable { get; set; }
    public string Uri { get; set; }
    public Aspect Aspect { get; set; }
}
