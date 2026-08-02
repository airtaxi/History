namespace History.Uno.ViewModels;

public class TextContentRun(string text, bool isBold, TextContentRunKind kind, string target = null, string colorHex = null)
{
    public string Text { get; set; } = text;
    public bool IsBold { get; } = isBold;
    public TextContentRunKind Kind { get; } = kind;

    /// <summary>
    /// Link URL, profile user id, or hashtag tag depending on <see cref="Kind"/>. Null for plain runs.
    /// </summary>
    public string Target { get; } = target;

    /// <summary>
    /// Optional ARGB hex color (e.g. "#999999") applied when rendering the run.
    /// </summary>
    public string ColorHex { get; } = colorHex;
}
