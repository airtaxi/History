namespace History.MobileClient.ContentViews;

public partial class DataTemplatePresenter : ContentView
{
    public static readonly BindableProperty ContentSelectorProperty =
        BindableProperty.Create(
            nameof(ContentSelector),
            typeof(DataTemplateSelector),
            typeof(DataTemplatePresenter),
            propertyChanged: OnSelectorChanged);

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

    public DataTemplateSelector ContentSelector
    {
        get => (DataTemplateSelector)GetValue(ContentSelectorProperty);
        set => SetValue(ContentSelectorProperty, value);
    }

    public DataTemplatePresenter()
    {
        InitializeComponent();
    }

    private static void OnSelectorChanged(BindableObject bindable, object oldValue, object newValue)
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
        if (ContentSelector == null || ViewModel == null)
            return;

        var template = ContentSelector.SelectTemplate(ViewModel, this);
        if (template?.CreateContent() is View view)
        {
            BindingContext = ViewModel;
            view.BindingContext = ViewModel;
            Content = view;
        }
    }
}
