namespace History.MobileClient.ViewModels;

public interface IMediaViewModel
{
    public string Uri { get; set; }
    public Aspect Aspect { get; set; }
    public string Description { get; set; }
    public bool HasDescription { get; }
}
