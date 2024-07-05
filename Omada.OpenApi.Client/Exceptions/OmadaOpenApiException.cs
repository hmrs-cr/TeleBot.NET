namespace Omada.OpenApi.Client.Exceptions;

public class OmadaOpenApiResponseException : Exception
{
    public OmadaOpenApiResponseException(string message, int errorCode) : base(message)
    {
        this.ErrorCode = errorCode;
    }

    public int ErrorCode { get; }
}
