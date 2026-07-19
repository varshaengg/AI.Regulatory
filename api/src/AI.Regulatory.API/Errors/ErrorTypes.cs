namespace AI.Regulatory.API.Errors;

/// <summary>
/// Slugs that map to `type` URIs in Problem Details responses.
/// See docs/API-Design.md §5.
/// </summary>
public static class ErrorTypes
{
    private const string Base = "https://ai-regulatory.example/errors/";

    public static readonly string Validation         = Base + "validation";
    public static readonly string Unauthenticated    = Base + "unauthenticated";
    public static readonly string Forbidden          = Base + "forbidden";
    public static readonly string NotFound           = Base + "not-found";
    public static readonly string Conflict           = Base + "conflict";
    public static readonly string PreconditionFailed = Base + "precondition-failed";
    public static readonly string Unprocessable      = Base + "unprocessable";
    public static readonly string TooManyRequests    = Base + "too-many-requests";
    public static readonly string Internal           = Base + "internal";
    public static readonly string Upstream           = Base + "upstream";
    public static readonly string Timeout            = Base + "timeout";
}
