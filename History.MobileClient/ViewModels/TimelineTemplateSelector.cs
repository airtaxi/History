namespace History.MobileClient.ViewModels;

internal class TimelineTemplateSelector : DataTemplateSelector
{
    public DataTemplate ProfileTemplate { get; set; }
    public DataTemplate PostTemplate { get; set; }
    public DataTemplate RepostTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is HistoryRepostViewModel) return RepostTemplate;
        else if (item is HistoryPostViewModel) return PostTemplate;
        else if (item is KakaoPostViewModel) return PostTemplate;
        else if (item is ProfileViewModel) return ProfileTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
