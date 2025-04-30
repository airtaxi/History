using Microsoft.Maui.Controls.Handlers.Items;

namespace History.MobileClient.ThirdParty.StaggeredLayout;

public class StaggeredStructuredItemsViewHandler : StructuredItemsViewHandler<CollectionView>
{
    public StaggeredStructuredItemsViewHandler() : base(StaggeredStructuredItemsViewMapper)
    {
    }
    public StaggeredStructuredItemsViewHandler(PropertyMapper mapper = null) : base(mapper ?? StaggeredStructuredItemsViewMapper)
    {
    }
    public static PropertyMapper<CollectionView, StructuredItemsViewHandler<CollectionView>> StaggeredStructuredItemsViewMapper = new(StructuredItemsViewMapper)
    {
        [StructuredItemsView.ItemsLayoutProperty.PropertyName] = MapItemsLayout
    };
#if ANDROID
    private static void MapItemsLayout(StructuredItemsViewHandler<CollectionView> handler, CollectionView view)
    {
        var platformView = handler.PlatformView as MauiRecyclerView<CollectionView, ItemsViewAdapter<CollectionView, IItemsViewSource>, IItemsViewSource>;
        switch (view.ItemsLayout)
        {
            case StaggeredItemsLayout staggeredItemsLayout:
                platformView?.UpdateAdapter();
                platformView?.SetLayoutManager(
                    new AndroidX.RecyclerView.Widget.StaggeredGridLayoutManager(
                        staggeredItemsLayout.Span,
                        staggeredItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal ? AndroidX.RecyclerView.Widget.StaggeredGridLayoutManager.Horizontal : AndroidX.RecyclerView.Widget.StaggeredGridLayoutManager.Vertical));
                break;
            default:
                platformView?.UpdateLayoutManager();
                break;
        }
    }
#endif
#if IOS || MACCATALYST
protected override ItemsViewLayout SelectLayout()
{
    var itemsLayout = ItemsView.ItemsLayout;
    if (itemsLayout is StaggeredItemsLayout staggeredItemsLayout)
    {
        return new StaggeredItemsViewLayout(staggeredItemsLayout, ItemSizingStrategy.MeasureAllItems);
    }
    return base.SelectLayout();
}
#endif
}
