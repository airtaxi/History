namespace History.MobileClient.ContentViews;

public partial class DataTemplatePresenter : ContentView
{
    public static readonly BindableProperty TemplateProperty =
        BindableProperty.Create(
            nameof(Template),
            typeof(DataTemplate),
            typeof(DataTemplatePresenter),
            propertyChanged: OnTemplateChanged);

    public static readonly BindableProperty ViewModelProperty =
        BindableProperty.Create(
            nameof(ViewModel),
            typeof(object),
            typeof(DataTemplatePresenter),
            propertyChanged: OnViewModelChanged);

    public object ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public DataTemplate Template
    {
        get => (DataTemplate)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public DataTemplatePresenter()
    {
        InitializeComponent();
    }

    private View _currentView;
    private DataTemplate _currentTemplate;
    private object _currentViewModel;

    private static void OnTemplateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DataTemplatePresenter presenter)
        {
            presenter.ApplyTemplate();
        }
    }

    private static void OnViewModelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DataTemplatePresenter presenter)
        {
            presenter.ApplyTemplate();
        }
    }

    private void ApplyTemplate()
    {
        if (Template == null || ViewModel == null)
        {
            Content = null;
            _currentView = null;
            _currentTemplate = null;
            _currentViewModel = null;
            return;
        }

        // Resolve the effective template (selector or direct).
        var effectiveTemplate = Template is DataTemplateSelector selector ? selector.SelectTemplate(ViewModel, this) : Template;

        // If only the ViewModel changed and the template is the same, reuse the existing view.
        if (_currentView != null && effectiveTemplate == _currentTemplate && ViewModel.GetType() == _currentViewModel?.GetType())
        {
            _currentView.BindingContext = ViewModel;
            _currentViewModel = ViewModel;
            return;
        }

        // Template or ViewModel type changed — create a new view.
        if (effectiveTemplate?.CreateContent() is View view)
        {
            view.BindingContext = ViewModel;
            Content = view;
            _currentView = view;
            _currentTemplate = effectiveTemplate;
            _currentViewModel = ViewModel;
        }
    }
}
