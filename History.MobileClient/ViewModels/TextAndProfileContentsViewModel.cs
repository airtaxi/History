using History.Commons.DataTypes.Contents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels
{
    public class TextAndProfileContentsViewModel(List<BaseContent> textAndProfileContents) : IContentViewModel
    {
        public FormattedString FormattedString { get; set; } = Utils.GenerateSpanFromTextAndProfileContents(textAndProfileContents);
    }
}
