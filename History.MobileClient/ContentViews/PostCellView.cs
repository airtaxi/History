using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using History.MobileClient.ViewModels;

namespace History.MobileClient.ContentViews;

public class PostCellView : ContentView
{
    // Debug flash channels: each toggles between 0xFF (on) and 0x60 (off) and composites into one color.
    // Measure caching is intentionally disabled to observe raw overhead.
    private bool _measureOverrideToggle;    // Blue channel
    private bool _invalidateMeasureToggle;  // Red channel
    private bool _manualToggle;             // Green channel

    public static readonly BindableProperty PostViewModelProperty =
        BindableProperty.Create(
            nameof(PostViewModel),
            typeof(PostViewModel),
            typeof(PostCellView),
            null,
            propertyChanged: OnPostViewModelChanged);

    public PostViewModel PostViewModel
    {
        get => (PostViewModel)GetValue(PostViewModelProperty);
        set => SetValue(PostViewModelProperty, value);
    }

    public PostCellView()
    {
        WeakReferenceMessenger.Default.Register<PostCellMeasureInvalidationMessage>(this, OnPostCellMeasureInvalidationReceived);
    }

    private void OnPostCellMeasureInvalidationReceived(object recipient, PostCellMeasureInvalidationMessage message)
    {
        if (PostViewModel != null && PostViewModel.Post.Id == message.Value) InvalidateMeasureManual();
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        _measureOverrideToggle = !_measureOverrideToggle;
        UpdateFlashColor();

        return base.MeasureOverride(widthConstraint, heightConstraint);
    }

    protected override void InvalidateMeasure()
    {
        // Block child InvalidateMeasure cascade from propagating to parent (CollectionView handler).
        _invalidateMeasureToggle = !_invalidateMeasureToggle;
        UpdateFlashColor();
    }

    public void InvalidateMeasureManual()
    {
        // Explicit re-measure path (UpdatePost / cell recycling). Allows propagation.
        _manualToggle = !_manualToggle;
        UpdateFlashColor();
        base.InvalidateMeasure();
    }

    private void UpdateFlashColor()
    {
        byte red = _invalidateMeasureToggle ? (byte)0xFF : (byte)0x60;
        byte green = _manualToggle ? (byte)0xFF : (byte)0x60;
        byte blue = _measureOverrideToggle ? (byte)0xFF : (byte)0x60;
        var color = Color.FromRgb(red, green, blue);
        MainThread.BeginInvokeOnMainThread(() => BackgroundColor = color);
    }

    private static void OnPostViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PostCellView view) view.InvalidateMeasureManual();
    }
}