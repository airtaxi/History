namespace History.WindowsClient.Models;

public sealed class ImagePasteRequestedEventArgs(byte[] imageData, string contentType, int cursorPosition) : EventArgs
{
    public byte[] ImageData { get; } = imageData;
    public string ContentType { get; } = contentType;
    public int CursorPosition { get; } = cursorPosition;
}