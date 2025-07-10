
namespace History.MobileClient.ContentViews;

public class SquareView : ContentView
{
    public SquareView()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, EventArgs e) => Resize();

    private void Resize()
    {
        var carouselView = FindCollectionView(this);
        if (carouselView == null) return;

        var layout = carouselView.ItemsLayout as GridItemsLayout;
        if (layout == null) return;

        HeightRequest = carouselView.Width / layout.Span;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
        {
            HeightRequest = width;
        }
        else Resize();
    }

    private static CollectionView FindCollectionView(View view)
    {
        var parent = view.Parent;
        while (parent != null && parent is not CollectionView) parent = parent.Parent;

        return parent as CollectionView;
    }
}