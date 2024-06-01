using Microsoft.Extensions.Logging;

namespace Linkplay.HttpApi;

public static class LinkplayHttpApiClientExtensions
{
    public static ILinkplayHttpApiClient SetTimeout(this ILinkplayHttpApiClient client, TimeSpan timeout)
    {
        if (timeout.TotalMilliseconds > 0)
        {
            client.RequestTimeout = timeout;
        }

        return client;
    }

    public static ILinkplayHttpApiClient SetLogger(this ILinkplayHttpApiClient client, ILogger? logger)
    {
        client.Logger = logger;
        return client;
    }
}
