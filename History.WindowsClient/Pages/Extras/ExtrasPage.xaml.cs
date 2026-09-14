using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class ExtrasPage : BasePage
{
    protected override ExtrasPageViewModel ViewModel { get; }

    public ExtrasPage()
    {
        ViewModel = App.Services.GetRequiredService<ExtrasPageViewModel>();

        InitializeComponent();
    }
}
