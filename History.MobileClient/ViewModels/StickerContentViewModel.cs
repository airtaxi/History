using History.Commons;
using History.Commons.DataTypes.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public class StickerContentViewModel(StickerContent stickerContent) : IContentViewModel
{
    public string StickerId => stickerContent.StickerId;
    public string StickerContentId => stickerContent.StickerContentId;
    public ImageViewModel Media { get; } = new ImageViewModel(CommonsConstants.MediaBaseUrl + stickerContent.StickerMediaId);
}
