using System.Diagnostics;
using System.Text.Json;

namespace TeleBotService.Extensions;

public static class ProcessExtensions
{
    public static ILogger? Logger { get; set; } = TelebotServiceApp.Logger;

    private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<T?> ExecuteJsonProcess<T>(this Process process, CancellationToken cancellationToken = default)
    {
        var result = await process.ExecuteProcess(cancellationToken);
        return result != null ? JsonSerializer.Deserialize<T>(result, jsonSerializerOptions) : default;
    }

    public static async Task<string?> ExecuteProcess(this Process process, CancellationToken cancellationToken = default)
    {
        var exitCode = await process.StartAndAwaitUntilExit(cancellationToken);
        var result = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        return exitCode == 0 ? result : null;
    }

    public static Process CreateProcessCommand(string command, string arguments) => new()
    {
        StartInfo =
        {
            FileName = command,
            UseShellExecute = false,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }
    };

    public static async Task<int> StartAndAwaitUntilExit(this Process process, CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("Executing command: {command} {arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    public static Task<int> ExecuteProcessCommand(string command, string arguments)
    {
        var process = CreateProcessCommand(command, arguments);
        return process.StartAndAwaitUntilExit();
    }

    public static async Task<string?> ExecuteProcessCommand(string command, string arguments, CancellationToken cancellationToken = default)
    {
        using var process = CreateProcessCommand(command, arguments);
        var result = await ExecuteProcess(process, cancellationToken);
        var errorResult = await process.StandardError.ReadToEndAsync(cancellationToken);
        Logger?.LogInformation("{command} {arguments}\n{result}", command, arguments, result);
        if (!string.IsNullOrEmpty(errorResult))
        {
            Logger?.LogInformation(errorResult);
        }
        return result;
    }

    public static async Task<T?> ExecuteJsonProcessCommand<T>(string command, string arguments, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteProcessCommand(command, arguments, cancellationToken);
        return result != null ? JsonSerializer.Deserialize<T>(result, jsonSerializerOptions) : default;
    }

    public static async Task Retry(this Func<CancellationToken, ValueTask> asyncAction, int retryTimes = 3, int waitTime = 1000, CancellationToken cancellationToken = default)
    {
        for (var c = 0; c < retryTimes - 1; c++)
        {
            try
            {
                await asyncAction.Invoke(cancellationToken);
                break;
            }
            catch
            {
                await Task.Delay(waitTime, cancellationToken);
            }
        }

        await asyncAction.Invoke(cancellationToken);
    }

}
