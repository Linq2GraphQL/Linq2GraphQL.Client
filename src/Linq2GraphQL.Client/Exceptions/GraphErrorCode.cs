namespace Linq2GraphQL.Client;

public enum GraphErrorCode
{
    Unknown,
    Authentication,
    Authorization,
    Forbidden,
    Validation,
    BadRequest,
    NotFound,
    RateLimited,
    InternalServerError,
    Timeout,
    Conflict,
    PersistedQueryNotFound,
    PersistedQueryMismatch
}

internal static class GraphErrorCodeClassifier
{
    private static readonly Dictionary<string, GraphErrorCode> CodeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Apollo / GraphQL-over-HTTP codes
        { "UNAUTHENTICATED", GraphErrorCode.Authentication },
        { "FORBIDDEN", GraphErrorCode.Forbidden },
        { "BAD_USER_INPUT", GraphErrorCode.BadRequest },
        { "VALIDATION_FAILED", GraphErrorCode.Validation },
        { "GRAPHQL_VALIDATION_FAILED", GraphErrorCode.Validation },
        { "INTERNAL_SERVER_ERROR", GraphErrorCode.InternalServerError },
        { "PERSISTED_QUERY_NOT_FOUND", GraphErrorCode.PersistedQueryNotFound },
        { "PERSISTED_QUERY_NOT_SUPPORTED", GraphErrorCode.PersistedQueryMismatch },
        // Hot Chocolate codes
        { "AUTH_NOT_AUTHENTICATED", GraphErrorCode.Authentication },
        { "AUTH_NOT_AUTHORIZED", GraphErrorCode.Authorization },
        { "HC0018", GraphErrorCode.Validation },
        { "HC0008", GraphErrorCode.BadRequest },
        { "HC0012", GraphErrorCode.NotFound },
        { "HC0001", GraphErrorCode.InternalServerError },
        // Common patterns
        { "RATE_LIMITED", GraphErrorCode.RateLimited },
        { "RATE_LIMIT_EXCEEDED", GraphErrorCode.RateLimited },
        { "THROTTLED", GraphErrorCode.RateLimited },
        { "TIMEOUT", GraphErrorCode.Timeout },
        { "REQUEST_TIMEOUT", GraphErrorCode.Timeout },
        { "CONFLICT", GraphErrorCode.Conflict },
        { "NOT_FOUND", GraphErrorCode.NotFound },
    };

    internal static GraphErrorCode Classify(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return GraphErrorCode.Unknown;
        return CodeMap.TryGetValue(code, out var errorCode) ? errorCode : GraphErrorCode.Unknown;
    }
}