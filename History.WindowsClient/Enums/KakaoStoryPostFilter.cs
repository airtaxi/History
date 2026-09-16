namespace History.WindowsClient.Enums;

// Which of the signed-in account's own posts the story batch operation targets. The order
// matches the filter selector's items.
public enum KakaoStoryPostFilter
{
    All,
    Public,
    Friends,
    OnlyMe,
    Blinded
}
