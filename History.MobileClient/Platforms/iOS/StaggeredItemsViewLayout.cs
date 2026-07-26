using System.Diagnostics;
using CoreGraphics;
using Foundation;
using History.MobileClient.ThirdParty.StaggeredLayout;
using Microsoft.Maui.Controls.Handlers.Items;
using UIKit;

namespace History.MobileClient;

public class StaggeredItemsViewLayout(StaggeredItemsLayout itemsLayout, ItemSizingStrategy sizingStrategy)
: ItemsViewLayout(itemsLayout, sizingStrategy)
{
    private const string Tag = "[StaggeredLayout]";

    private List<UICollectionViewLayoutAttributes> cache = [];

    private int cachedItemCount = -1;

    private bool needsRebuild = true;

    private float contentHeight;

    public int ColumnCount { get; set; } = Math.Max(1, itemsLayout.Span);

    public double MinCellHeight { get; set; } = 150;

    // Guard against nil context that causes NSInvalidArgumentException during insertion animations
    public override void InvalidateLayout(UICollectionViewLayoutInvalidationContext context)
    {
        if (context is null) return;
        needsRebuild = true;
        base.InvalidateLayout(context);
    }

    // Invalidate on width change (rotation, resize) but not on scroll
    public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
    {
        if (CollectionView is null) return false;
        return Math.Abs(newBounds.Width - CollectionView.Bounds.Width) > 0.1;
    }

    public override CGSize CollectionViewContentSize =>
        CollectionView is null ? CGSize.Empty : new(CollectionView.Frame.Width, contentHeight);

    public override void ConstrainTo(CGSize size)
    {
        ConstrainedDimension = ScrollDirection == UICollectionViewScrollDirection.Vertical ? size.Width : size.Height;
        DetermineCellSize();
    }

    public override void PrepareLayout()
    {
        // Let base ItemsViewLayout measure items and prepare its internal cache.
        // MAUI's ItemsViewLayout uses GetSizeForItem to measure each cell.
        base.PrepareLayout();

        if (CollectionView is null || ColumnCount <= 0) return;

        var itemsCount = CollectionView.NumberOfItemsInSection(0).ToInt32();

        if (itemsCount != cachedItemCount)
        {
            cachedItemCount = itemsCount;
            needsRebuild = true;
        }

        if (!needsRebuild && cache.Count == itemsCount) return;
        needsRebuild = false;

        Debug.WriteLine($"{Tag} PrepareLayout: rebuilding (items={itemsCount}, cols={ColumnCount})");

        cache.Clear();
        contentHeight = 0;

        if (itemsCount == 0) return;

        var colWidth = (float)CollectionView.Frame.Width / ColumnCount;
        var colHeights = new float[ColumnCount];

        for (var i = 0; i < itemsCount; i++)
        {
            var indexPath = NSIndexPath.FromRowSection(i, 0);

            // Get MAUI's measured height from base layout's attributes
            var baseAttr = base.LayoutAttributesForItem(indexPath);
            var height = baseAttr != null ? (float)baseAttr.Frame.Height : (float)MinCellHeight;

            if (i < 5)
                Debug.WriteLine($"{Tag} item {i}: height={height:F1}");

            // Assign to shortest column for balanced staggered layout
            var col = 0;
            for (var c = 1; c < ColumnCount; c++)
            {
                if (colHeights[c] < colHeights[col]) col = c;
            }

            var frame = new CGRect(col * colWidth, colHeights[col], colWidth, height);
            var attr = UICollectionViewLayoutAttributes.CreateForCell(indexPath);
            attr.Frame = frame;
            cache.Add(attr);

            colHeights[col] += height;
            contentHeight = Math.Max(contentHeight, colHeights[col]);
        }

        Debug.WriteLine($"{Tag} PrepareLayout: done (contentHeight={contentHeight})");
    }

    public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
    {
        var visible = new List<UICollectionViewLayoutAttributes>();

        foreach (var attr in cache)
        {
            if (attr.Frame.IntersectsWith(rect)) visible.Add(attr);
        }

        return [.. visible];
    }

    public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
    {
        if (indexPath is null) return null;

        var row = indexPath.Row;
        if (row >= 0 && row < cache.Count) return cache[row];

        // Fallback to base during initial PrepareLayout before cache is built
        return base.LayoutAttributesForItem(indexPath);
    }
}