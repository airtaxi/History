namespace History.MobileClient.ViewModels;

internal class TimelineTemplateSelector : DataTemplateSelector
{
    public DataTemplate ProfileTemplate { get; set; }
    public DataTemplate PostTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ProfileViewModel) return ProfileTemplate;
        else if (item is PostViewModel) return PostTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
