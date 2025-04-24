
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using SpeakLink.Mention;

namespace History.MobileClient.Pages;

public partial class EditPostPage : ContentPage
{
	public EditPostPage()
    {
        InitializeComponent();
        Initialize();
    }

    public EditPostPage(string postId)
    {
        InitializeComponent();
        Initialize();
    }

    private void Initialize()
    {
        DiscoveryOptionPicker.ItemsSource = Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString()).ToList();
    }
}