using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace History.WindowsClient;

public static class Utils
{
    // Adds a clickable item that runs the given async action when tapped.
    public static MenuFlyoutItem CreateActionItem(string text, string glyph, Func<Task> action, Windows.UI.Color? iconColor = null)
    {
        var item = new MenuFlyoutItem { Text = text, Tag = action };
        if (iconColor != null) item.Icon = new FontIcon { Glyph = glyph, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(iconColor.Value) };
        else item.Icon = new FontIcon { Glyph = glyph };
        item.Click += async (sender, _) => await ((Func<Task>)((MenuFlyoutItem)sender).Tag)();
        return item;
    }

    // Adds a clickable item that runs the given synchronous action when tapped.
    public static MenuFlyoutItem CreateActionItem(string text, string glyph, Action action, Windows.UI.Color? iconColor = null)
    {
        var item = new MenuFlyoutItem { Text = text, Tag = action };
        if (iconColor != null) item.Icon = new FontIcon { Glyph = glyph, Foreground = new SolidColorBrush(iconColor.Value) };
        else item.Icon = new FontIcon { Glyph = glyph };
        item.Click += (sender, _) => ((Action)((MenuFlyoutItem)sender).Tag)();
        return item;
    }
}