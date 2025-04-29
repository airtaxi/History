using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.ApiService.DataTypes;

public class DataWithFilesForm
{
    public string JsonData { get; set; }
    public List<IFormFile> Files { get; set; }
}
