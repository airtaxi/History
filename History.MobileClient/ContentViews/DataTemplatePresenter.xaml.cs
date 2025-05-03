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
            return;

        if(Template is DataTemplateSelector selector)
        {
            var template = selector.SelectTemplate(ViewModel, this);
            if (template?.CreateContent() is View view)
            {
                BindingContext = ViewModel;
                view.BindingContext = ViewModel;
                Content = view;
            }
        }
        else
        {
            if (Template.CreateContent() is View view)
            {
                BindingContext = ViewModel;
                view.BindingContext = ViewModel;
                Content = view;
            }
        }
    }
}
