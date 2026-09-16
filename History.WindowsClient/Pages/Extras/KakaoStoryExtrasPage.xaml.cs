using History.WindowsClient.ViewModels.Extras;
using Microsoft.Extensions.DependencyInjection;

namespace History.WindowsClient.Pages.Extras;

public sealed partial class KakaoStoryExtrasPage : BasePage
{
    protected override KakaoStoryExtrasPageViewModel ViewModel { get; }

    public KakaoStoryExtrasPage()
    {
        ViewModel = App.Services.GetRequiredService<KakaoStoryExtrasPageViewModel>();

        InitializeComponent();
    }
}
