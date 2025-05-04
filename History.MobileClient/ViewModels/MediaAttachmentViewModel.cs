using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class MediaAttachmentViewModel : ObservableObject, IDisposable
{
    public byte[] Data { get; }
    public MediaContent ServerContent { get; }
    public bool IsUpload => ServerContent == null;
    public string FileName { get; }
    public ImageSource ImageSource { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionEmpty))]
    public partial string Description { get; set; } = string.Empty;

    public bool IsDescriptionEmpty => string.IsNullOrEmpty(Description);

    private readonly string _filePath = Path.GetTempFileName();

    public MediaAttachmentViewModel(MediaContent serverContent)
    {
        ServerContent = serverContent;

        // Set Description
        Description = serverContent.Description ?? string.Empty;

        // Set ImageSource
        if (serverContent.IsVideo) ImageSource = ImageSource.FromFile("video.png");
        else ImageSource = ImageSource.FromUri(new(Utils.GenerateMediaUri(serverContent.MediaId)));
    }

    public MediaAttachmentViewModel(string fileName, byte[] imageBytes, bool isVideo = false)
    {
        Data = imageBytes;
        FileName = fileName;
        if (!isVideo)
        {
            File.WriteAllBytes(_filePath, imageBytes);
            ImageSource = ImageSource.FromFile(_filePath);
        }
        else ImageSource = ImageSource.FromFile("video.png");
    }

    public void Dispose()
    {
        File.Delete(_filePath);
        GC.SuppressFinalize(this);
    }
}
