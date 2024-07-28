using System.Text;
using System.Text.Json;
using TeleBotService.Core.Model;

namespace TeleBotService.Core.Commands.Admin;

public class JobInfoCommand : TelegramCommand
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IJobInfoProvider jobInfoProvider;

    public JobInfoCommand(IJobInfoProvider jobInfoProvider)
    {
        this.jobInfoProvider = jobInfoProvider;
    }

    public override string CommandString => "/GetJobs";

    public override bool IsAdmin => true;

    protected override async Task Execute(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var jobs = this.jobInfoProvider.GetJobs();
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(utf8Json: stream, jobs, this.jsonSerializerOptions, cancellationToken: cancellationToken);
        stream.Position = 0;
        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        string result = streamReader.ReadToEnd();

        var sb = new StringBuilder();
        sb.Append("<pre>").Append(result).Append("</pre>");
        await this.ReplyFormated(messageContext, sb.ToString(), cancellationToken);
    }
}
