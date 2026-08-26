using History.WindowsClient.Models;
using History.WindowsClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.Helpers;

public static class DialogHelper
{
    private const string DefaultOkButtonText = "확인";
    private const string DefaultCancelButtonText = "취소";
    private const string DefaultInputTitleText = "입력";

    private static readonly Lazy<ApplicationThemeService> s_applicationThemeServiceLazy = new(() => App.Services.GetRequiredService<ApplicationThemeService>());
    private static ApplicationThemeService s_applicationThemeService => s_applicationThemeServiceLazy.Value;

    public static async Task<string> ShowInputDialogAsync(this UIElement element, InputDialogParameters parameters)
    {
        HideOpenContentDialogs(element);

        var dialog = new ContentDialog
        {
            Title = parameters.Title ?? DefaultInputTitleText,
            PrimaryButtonText = DefaultOkButtonText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = element.XamlRoot
        };
        s_applicationThemeService.RegisterThemeTarget(dialog);

        var textBox = new TextBox();
        if (parameters.NumberOnly) textBox.BeforeTextChanging += (textBoxSender, textBoxBeforeTextChangingEventArguments) => textBoxBeforeTextChangingEventArguments.Cancel = textBoxBeforeTextChangingEventArguments.NewText.Any(character => !char.IsDigit(character));
        textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        textBox.PlaceholderText = parameters.PlaceholderText;
        if (!string.IsNullOrEmpty(parameters.DefaultText)) textBox.Text = parameters.DefaultText;
        dialog.Content = textBox;

        if (parameters.ShowCancel) dialog.SecondaryButtonText = DefaultCancelButtonText;

        TaskCompletionSource<string> taskCompletionSource = new();
        dialog.Closing += (contentDialogSender, contentDialogClosingEventArguments) =>
        {
            taskCompletionSource.SetResult(contentDialogClosingEventArguments.Result == ContentDialogResult.Primary ? textBox.Text.Trim() : null);
        };
        await dialog.ShowAsync();
        return await taskCompletionSource.Task;
    }

    public static ContentDialog GenerateMessageDialog(this UIElement element, MessageDialogParameters parameters)
    {
        HideOpenContentDialogs(element);

        var xamlRoot = element.XamlRoot;
        var dialog = new ContentDialog
        {
            Title = parameters.Title,
            Content = parameters.Description,
            PrimaryButtonText = parameters.PrimaryButtonText ?? DefaultOkButtonText,
            XamlRoot = xamlRoot,
            DefaultButton = ContentDialogButton.Primary
        };
        s_applicationThemeService.RegisterThemeTarget(dialog);

        if (!string.IsNullOrEmpty(parameters.SecondaryButtonText)) dialog.SecondaryButtonText = parameters.SecondaryButtonText;
        return dialog;
    }

    public static async Task<ContentDialogResult> ShowMessageDialogAsync(this UIElement element, MessageDialogParameters parameters)
    {
        var dialog = GenerateMessageDialog(element, parameters);
        return await dialog.ShowAsync();
    }

    private static void HideOpenContentDialogs(UIElement element)
    {
        var contentDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot).Where(popup => popup.Child is ContentDialog).Select(popup => popup.Child as ContentDialog);
        if (!contentDialogs.Any()) return;

        foreach (var contentDialog in contentDialogs) contentDialog.Hide();
    }
}
