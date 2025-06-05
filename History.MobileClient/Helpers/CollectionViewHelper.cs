
using Microsoft.Maui.Controls.Handlers.Items;

#if ANDROID
using AndroidX.RecyclerView.Widget;
using Android.Views;
#elif IOS
using UIKit;
using Foundation;
#endif

namespace History.MobileClient.Helpers;

public static class CollectionViewHelper
{
    /// <summary>
    /// Gets the current Y-axis scroll offset of the CollectionView.
    /// </summary>
    /// <param name="collectionView">Target CollectionView</param>
    /// <returns>Y-axis scroll offset</returns>
    public static double GetScrollOffsetY(this CollectionView collectionView)
    {
        if (collectionView?.Handler is not StructuredItemsViewHandler<CollectionView> handler)
            return 0;

#if ANDROID
        if (handler.PlatformView is RecyclerView recyclerView)
        {
            var layoutManager = recyclerView.GetLayoutManager();
                
            if (layoutManager is LinearLayoutManager linearLayoutManager)
            {
                var firstVisibleItemPosition = linearLayoutManager.FindFirstVisibleItemPosition();
                var firstVisibleView = layoutManager.FindViewByPosition(firstVisibleItemPosition);
                    
                if (firstVisibleView != null)
                {
                    var offset = firstVisibleView.Top;
                    var itemHeight = firstVisibleView.Height;
                    return (firstVisibleItemPosition * itemHeight) - offset;
                }
            }
            else if (layoutManager is GridLayoutManager || layoutManager is StaggeredGridLayoutManager)
            {
                // Use ComputeVerticalScrollOffset for Grid and StaggeredGrid layouts
                return recyclerView.ComputeVerticalScrollOffset();
            }
                
            return recyclerView.ComputeVerticalScrollOffset();
        }

#elif IOS
        if (handler.PlatformView is UICollectionView uiCollectionView)
        {
            return uiCollectionView.ContentOffset.Y;
        }
#endif
        return 0;
    }

    /// <summary>
    /// Sets the Y-axis scroll offset of the CollectionView.
    /// </summary>
    /// <param name="collectionView">Target CollectionView</param>
    /// <param name="offsetY">Y-axis offset</param>
    /// <param name="isAnimated">Whether to animate (default: true)</param>
    public static void SetScrollOffsetY(this CollectionView collectionView, double offsetY, bool isAnimated = true)
    {
        if (collectionView?.Handler is not StructuredItemsViewHandler<CollectionView> handler)
            return;

#if ANDROID
        if (handler.PlatformView is RecyclerView recyclerView)
        {
            // Stop any ongoing scroll animation to prevent momentum interference
            recyclerView.StopScroll();

            if (isAnimated)
            {
                var currentY = recyclerView.ComputeVerticalScrollOffset();
                var deltaY = offsetY - currentY;
                recyclerView.SmoothScrollBy(0, (int)deltaY);
            }
            else
            {
                var currentY = recyclerView.ComputeVerticalScrollOffset();
                var deltaY = offsetY - currentY;
                recyclerView.ScrollBy(0, (int)deltaY);
            }
        }

#elif IOS
        if (handler.PlatformView is UICollectionView uiCollectionView)
        {
            // Stop any ongoing scroll animation to prevent momentum interference
            if (uiCollectionView.Dragging || uiCollectionView.Decelerating)
            {
                uiCollectionView.SetContentOffset(uiCollectionView.ContentOffset, false);
            }
                
            var currentOffset = uiCollectionView.ContentOffset;
            var newOffset = new CoreGraphics.CGPoint(currentOffset.X, offsetY);
            uiCollectionView.SetContentOffset(newOffset, isAnimated);
        }
#endif
    }

    /// <summary>
    /// Gets the maximum Y-axis scroll range of the CollectionView.
    /// </summary>
    /// <param name="collectionView">Target CollectionView</param>
    /// <returns>Maximum Y-axis scroll range</returns>
    public static double GetMaxScrollOffsetY(this CollectionView collectionView)
    {
        if (collectionView?.Handler is not StructuredItemsViewHandler<CollectionView> handler)
            return 0;

#if ANDROID
        if (handler.PlatformView is RecyclerView recyclerView)
        {
            return Math.Max(0, recyclerView.ComputeVerticalScrollRange() - 
                            recyclerView.ComputeVerticalScrollExtent());
        }

#elif IOS
        if (handler.PlatformView is UICollectionView uiCollectionView)
        {
            var contentSize = uiCollectionView.ContentSize;
            var frameSize = uiCollectionView.Frame.Size;
            var contentInset = uiCollectionView.ContentInset;

            return Math.Max(0, contentSize.Height - frameSize.Height +
                            contentInset.Top + contentInset.Bottom);
        }
#endif
        return 0;
    }

    /// <summary>
    /// Checks if the CollectionView can scroll vertically.
    /// </summary>
    /// <param name="collectionView">Target CollectionView</param>
    /// <returns>Whether vertical scrolling is possible</returns>
    public static bool CanScrollVertically(this CollectionView collectionView)
    {
        if (collectionView?.Handler is not StructuredItemsViewHandler<CollectionView> handler)
            return false;

#if ANDROID
        if (handler.PlatformView is RecyclerView recyclerView)
        {
            return recyclerView.CanScrollVertically(1) || recyclerView.CanScrollVertically(-1);
        }

#elif IOS
        if (handler.PlatformView is UICollectionView uiCollectionView)
        {
            var contentSize = uiCollectionView.ContentSize;
            var frameSize = uiCollectionView.Frame.Size;

            return contentSize.Height > frameSize.Height;
        }
#endif
        return false;
    }

    /// <summary>
    /// Scrolls relative to the current position on the Y-axis.
    /// </summary>
    /// <param name="collectionView">Target CollectionView</param>
    /// <param name="deltaY">Y-axis movement amount</param>
    /// <param name="isAnimated">Whether to animate (default: true)</param>
    public static void ScrollByY(CollectionView collectionView, double deltaY, bool isAnimated = true)
    {
        if (collectionView?.Handler is not StructuredItemsViewHandler<CollectionView> handler)
            return;

#if ANDROID
        if (handler.PlatformView is RecyclerView recyclerView)
        {
            // Stop any ongoing scroll animation to prevent momentum interference
            recyclerView.StopScroll();

            if (isAnimated) recyclerView.SmoothScrollBy(0, (int)deltaY);
            else recyclerView.ScrollBy(0, (int)deltaY);
        }

#elif IOS
        if (handler.PlatformView is UICollectionView uiCollectionView)
        {
            // Stop any ongoing scroll animation to prevent momentum interference
            if (uiCollectionView.Dragging || uiCollectionView.Decelerating)
            {
                uiCollectionView.SetContentOffset(uiCollectionView.ContentOffset, false);
            }
        
            var currentOffset = uiCollectionView.ContentOffset;
            var newOffset = new CoreGraphics.CGPoint(currentOffset.X, currentOffset.Y + deltaY);
            uiCollectionView.SetContentOffset(newOffset, isAnimated);
        }
#endif
    }
}