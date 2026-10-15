using System.Net.Http.Headers;

namespace Linq2GraphQL.Client;

/// <summary>
/// Ready-made request hooks for <see cref="GraphClientOptions.ConfigureRequest"/> and
/// <see cref="GraphClient.WithRequestConfiguration"/>.
/// </summary>
public static class RequestConfigurations
{
    /// <summary>
    /// Sets <c>Authorization: Bearer &lt;token&gt;</c> on every request, asking <paramref name="getAccessToken"/>
    /// for the token each time. Caching and refreshing the token is the delegate's job, as token acquisition
    /// libraries already do.
    /// </summary>
    public static Func<HttpRequestMessage, CancellationToken, ValueTask> BearerToken(
        Func<CancellationToken, ValueTask<string>> getAccessToken)
    {
        ArgumentNullException.ThrowIfNull(getAccessToken);

        return async (request, cancellationToken) =>
        {
            var token = await getAccessToken(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        };
    }
}
