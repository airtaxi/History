namespace History.MobileClient.Pages;

public partial class UserPage : ContentPage
{
	private readonly string _userId;

	public UserPage()
	{
		_userId = Shared.UserId;
		InitializeComponent();
	}

	public UserPage(string userId)
	{
		_userId = userId;
        InitializeComponent();
    }
}