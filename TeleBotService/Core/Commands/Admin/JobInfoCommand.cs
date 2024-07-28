using System.Text;
using System.Text.Json;
using Humanizer;
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
        var sb = new StringBuilder().Append("<pre>");

        if (messageContext.Message.Text?.Contains("schedule", StringComparison.InvariantCultureIgnoreCase) == true)
        {
            await foreach (var job in jobs)
            {
                DateTimeOffset? previousFireTimeUtc = null;
                DateTimeOffset? nextFireTimeUtc = null;
                if (job.Triggers.FirstOrDefault() is { } defaultTrigger)
                {
                    previousFireTimeUtc = defaultTrigger.PreviousFireTimeUtc;
                    nextFireTimeUtc = defaultTrigger.NextFireTimeUtc;
                }

                sb.Append(job.JobKey).Append("  PrevExec: ").Append(previousFireTimeUtc.Humanize()).Append(", NexExec: ").Append(nextFireTimeUtc.Humanize())
                  .AppendLine();
            }
        }
        else
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(utf8Json: stream, jobs, this.jsonSerializerOptions, cancellationToken: cancellationToken);
            stream.Position = 0;
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var result = streamReader.ReadToEnd();
            sb.Append(result);
        }

        sb.Append("</pre>");
        await this.ReplyFormated(messageContext, sb.ToString(), cancellationToken);
    }
}
