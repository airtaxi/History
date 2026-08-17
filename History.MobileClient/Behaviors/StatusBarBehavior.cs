using System.Runtime.CompilerServices;

namespace History.MobileClient.Behaviors;

/// <summary>
/// Status bar icon theme. Light = dark icons (for light status bar backgrounds),
/// Dark = light icons (for dark status bar backgrounds).
/// </summary>
public enum StatusBarTheme
{
    /// <summary>
    /// Dark icons — use when the status bar background is light (e.g. White).
    /// </summary>
    Light = 0,

    /// <summary>
    /// Light icons — use when the status bar background is dark (e.g. OffBlack, Primary).
    /// </summary>
    Dark = 1
}

/// <summary>
/// PlatformBehavior that controls the status bar color and icon theme.
/// Automatically applies on page navigation (NavigatedTo) — no ApplyOn configuration needed.
/// Re-applies on display info changes (foldable screen resize / rotation) to fix Android sizing issues.
/// </summary>
public partial class StatusBarBehavior : PlatformBehavior<Page, object>
{
    /// <summary>
    /// Backing store for <see cref="StatusBarColor"/>.
    /// </summary>
    public static readonly BindableProperty StatusBarColorProperty = BindableProperty.Create(nameof(StatusBarColor), typeof(Color), typeof(StatusBarBehavior), Colors.Transparent);

    /// <summary>
    /// Backing store for <see cref="StatusBarTheme"/>.
    /// </summary>
    public static readonly BindableProperty StatusBarThemeProperty = BindableProperty.Create(nameof(StatusBarTheme), typeof(StatusBarTheme), typeof(StatusBarBehavior), StatusBarTheme.Light);

    /// <summary>
    /// The status bar background color. Supports AppThemeBinding.
    /// </summary>
    public Color StatusBarColor
    {
        get => (Color)GetValue(StatusBarColorProperty);
        set => SetValue(StatusBarColorProperty, value);
    }

    /// <summary>
    /// The status bar icon theme (Light = dark icons, Dark = light icons). Supports AppThemeBinding.
    /// </summary>
    public StatusBarTheme StatusBarTheme
    {
        get => (StatusBarTheme)GetValue(StatusBarThemeProperty);
        set => SetValue(StatusBarThemeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedTo(Page page, object platformView)
    {
        base.OnAttachedTo(page, platformView);

        page.NavigatedTo += OnPageNavigatedTo;
        DeviceDisplay.Current.MainDisplayInfoChanged += OnMainDisplayInfoChanged;
    }

    /// <inheritdoc />
    protected override void OnDetachedFrom(Page page, object platformView)
    {
        page.NavigatedTo -= OnPageNavigatedTo;
        DeviceDisplay.Current.MainDisplayInfoChanged -= OnMainDisplayInfoChanged;

        base.OnDetachedFrom(page, platformView);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (string.IsNullOrEmpty(propertyName)) return;

        if (propertyName == StatusBarColorProperty.PropertyName) PlatformSetColor(StatusBarColor);
        else if (propertyName == StatusBarThemeProperty.PropertyName) PlatformSetTheme(StatusBarTheme);
    }

    void OnPageNavigatedTo(object sender, NavigatedToEventArgs eventArgs)
    {
        PlatformSetColor(StatusBarColor);
        PlatformSetTheme(StatusBarTheme);
    }

    void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs eventArgs)
    {
        // Re-apply on foldable screen resize / rotation to keep the status bar in sync.
        PlatformSetColor(StatusBarColor);
        PlatformSetTheme(StatusBarTheme);
    }

    static partial void PlatformSetColor(Color color);
    static partial void PlatformSetTheme(StatusBarTheme theme);
}