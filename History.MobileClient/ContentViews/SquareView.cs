namespace History.MobileClient.ContentViews;

public class SquareView : ContentView
{
    public SquareView() => SizeChanged += OnSizeChanged;

    private void OnSizeChanged(object sender, EventArgs e) => HeightRequest = Width;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        HeightRequest = Width;
    }
}