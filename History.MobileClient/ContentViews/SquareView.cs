namespace History.MobileClient.ContentViews;

public class SquareView : ContentView
{
	public SquareView()
	{
		SizeChanged += OnSizeChanged;
        Unloaded += OnUnloaded;
	}

    private void OnUnloaded(object sender, EventArgs e)
    {
        Unloaded -= OnUnloaded;
        SizeChanged -= OnSizeChanged;
    }

    private void OnSizeChanged(object sender, EventArgs e) => HeightRequest = Width;
}