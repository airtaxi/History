using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public interface IMediaViewModel
{
    public string Uri { get; set; }
    public Aspect Aspect { get; set; }
    public string Description { get; set; }
    public bool HasDescription { get; }
}
