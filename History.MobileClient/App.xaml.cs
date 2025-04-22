using History.MobileClient.Pages;

namespace History.MobileClient;

public partial class App : Application
{
    public static Window MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        MainWindow = new Window(new LoginPage());
        return MainWindow;
    }
}