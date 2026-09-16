namespace History.WindowsClient.Enums;

// Which batch operation the shared story management page performs. The page wording,
// the confirmation flow, and the API call all follow this single value.
public enum KakaoStoryBatchManageMode
{
    Delete,
    ChangePermission
}
