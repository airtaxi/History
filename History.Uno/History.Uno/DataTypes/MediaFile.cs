namespace History.Uno.DataTypes;

public class MediaFile
{
    public string FileName { get; }
    public byte[] Bytes { get; }

    public MediaFile(string fileName, byte[] bytes)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("fileName cannot be null or empty.", nameof(fileName));

        FileName = fileName;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }
}