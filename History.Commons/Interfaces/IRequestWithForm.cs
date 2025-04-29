namespace History.Commons.Interfaces;

public interface IRequestWithForm : IBaseRequest
{
    public object Body { get; set; }
}
