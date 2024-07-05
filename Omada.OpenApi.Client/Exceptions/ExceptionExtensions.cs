namespace Omada.OpenApi.Client.Exceptions;

public static class ExceptionExtensions
{
    public static void ThrowIfFailed(this IResponse response)
    {
        if (!response.IsOk)
        {
            throw new OmadaOpenApiResponseException(response.Msg, response.ErrorCode);
        }
    }
}
