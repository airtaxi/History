namespace History.MobileClient.ContentViews;

public partial class DataTemplatePresenter : ContentView
{
    public static readonly BindableProperty ContentSelectorProperty =
        BindableProperty.Create(
            nameof(ContentSelector),
            typeof(DataTemplateSelector),
            typeof(DataTemplatePresenter),
            propertyChanged: OnSelectorChanged);

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

    private void OnBindingContextChanged(object sender, EventArgs e)
    {
        ApplyTemplate();
    }

    private void ApplyTemplate()
    {
        if (ContentSelector == null || BindingContext == null)
            return;

        var template = ContentSelector.SelectTemplate(BindingContext, this);
        if (template?.CreateContent() is View view)
        {
            view.BindingContext = BindingContext;
            Content = view;
        }
    }
}
