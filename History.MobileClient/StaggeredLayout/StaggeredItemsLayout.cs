using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.StaggeredLayout;

public class StaggeredItemsLayout(ItemsLayoutOrientation orientation) : ItemsLayout(orientation)
{
    public static readonly BindableProperty SpanProperty = BindableProperty.Create(nameof(Span), typeof(int), typeof(StaggeredItemsLayout), default(int));

    public StaggeredItemsLayout() : this(ItemsLayoutOrientation.Vertical)
    {

    }

    public int Span
    {
        get => (int)GetValue(SpanProperty);
        set => SetValue(SpanProperty, value);
    }
}
