namespace History.MobileClient.ContentViews;

public class SquareView : ContentView
{
    public SquareView()
    {
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        if (Width > 0)
        {
            HeightRequest = Width;
        }
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        if (Width > 0)
        {
            HeightRequest = Width;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
        {
            HeightRequest = width;
        }
    }
}