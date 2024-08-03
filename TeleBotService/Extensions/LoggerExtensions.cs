namespace TeleBotService.Extensions;

public static class LoggerExtensions
{
    public static void LogSimpleException(this ILogger logger, string message, Exception e) =>
        logger.LogWarning("{Message}: [{ExceptionType}] {ExceptionMessage}", message, e.GetType().FullName, e.Message);
}
