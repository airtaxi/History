namespace History.MobileClient.Pages;

public partial class FriendsPage : TabbedPage
{
	public FriendsPage()
	{
		InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopAsync();
        return true;
    }
}