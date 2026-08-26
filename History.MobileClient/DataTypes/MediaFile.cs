namespace History.MobileClient.DataTypes;

public class MediaFile
{
    public string FileName { get; }
    public byte[] Bytes { get; }
    public string FilePath { get; set; }
    public long Size { get; set; }

    public MediaFile(string fileName, byte[] bytes)
    {
        if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("fileName cannot be null or empty.", nameof(fileName));

        FileName = fileName;
        Bytes = bytes;
        Size = bytes?.Length ?? 0;
    }

    public byte[] GetBytes() => Bytes ?? (FilePath != null && File.Exists(FilePath) ? File.ReadAllBytes(FilePath) : []);
}
