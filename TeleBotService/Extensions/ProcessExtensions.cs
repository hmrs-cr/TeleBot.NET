using System.Diagnostics;
using System.Text.Json;

namespace TeleBotService;

public static class ProcessExtensions
{
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
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        var result = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        return process.ExitCode == 0 ? result : null;
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

    public static async Task<string?> ExecuteProcessCommand(string command, string arguments, CancellationToken cancellationToken = default)
    {
        using var process = CreateProcessCommand(command, arguments);
        var result = await ExecuteProcess(process, cancellationToken);
        var errorResult = await process.StandardError.ReadToEndAsync(cancellationToken);
        Console.WriteLine($"{command} {arguments}\n{result}");
        if (!string.IsNullOrEmpty(errorResult))
        {
            Console.WriteLine();
            Console.WriteLine(errorResult);
        }
        return result;
    }

    public static async Task<T?> ExecuteJsonProcessCommand<T>(string command, string arguments, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteProcessCommand(command, arguments, cancellationToken);
        return result != null ? JsonSerializer.Deserialize<T>(result, jsonSerializerOptions) : default;
    }
}
