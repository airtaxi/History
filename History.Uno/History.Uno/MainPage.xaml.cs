namespace History.MobileClient;

public sealed partial class MainPage : Page
{
    public static bool IsLoaded { get; private set; }
    public MainPage()
    {
        this.InitializeComponent();
#if ANDROID || IOS
        (MentionEditor.VisualElement as SpeakLink.Mention.MentionEditor).AutoSize = Microsoft.Maui.Controls.EditorAutoSizeOption.TextChanges;
#endif
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => IsLoaded = true;
}
