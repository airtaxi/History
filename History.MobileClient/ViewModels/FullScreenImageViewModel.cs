using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public sealed class FullScreenImageViewModel : ImageViewModel
{
    public FullScreenImageViewModel(ImageViewModel source) : base(source.Uri)
    {
        Aspect = Aspect.AspectFit;
    }
}