namespace History.Commons.Interfaces;

// Transport-neutral HTTP method for API requests. Only the verbs the API layer
// actually uses are listed; the handler maps them to System.Net.Http.HttpMethod.
public enum HttpRequestMethod
{
    Get,
    Post,
    Put,
    Delete
}
