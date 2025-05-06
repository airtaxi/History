namespace History.ApiService.DataTypes;

public class DataWithFilesForm
{
    public string JsonData { get; set; }
    public List<IFormFile> Files { get; set; }
}
