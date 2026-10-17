using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace AI.Regulatory.API.Data;

public sealed record PreparationRunMessage(string RunId);

/// <summary>Hands a created run to whatever executes its stages.</summary>
public interface IPreparationQueue
{
    Task EnqueueAsync(string runId, CancellationToken ct);
}

/// <summary>Publishes run commands to the Service Bus queue using the workload managed identity.</summary>
public sealed class ServiceBusPreparationQueue : IPreparationQueue, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusPreparationQueue(string fullyQualifiedNamespace, string queueName)
    {
        _client = new ServiceBusClient(fullyQualifiedNamespace, new DefaultAzureCredential());
        _sender = _client.CreateSender(queueName);
    }

    public Task EnqueueAsync(string runId, CancellationToken ct)
        => _sender.SendMessageAsync(
            new ServiceBusMessage(JsonSerializer.Serialize(new PreparationRunMessage(runId)))
            {
                MessageId = runId, // queue has duplicate detection enabled
                ContentType = "application/json",
            }, ct);

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}

/// <summary>
/// Fallback when Service Bus is not configured (local/mocked): runs the stages inside the API process
/// so the flow still works end to end.
/// </summary>
public sealed class InProcessPreparationQueue : IPreparationQueue
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InProcessPreparationQueue> _log;

    public InProcessPreparationQueue(IServiceProvider services, ILogger<InProcessPreparationQueue> log)
    {
        _services = services;
        _log = log;
    }

    public Task EnqueueAsync(string runId, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _services.GetRequiredService<PreparationRunService>().DiscoverAsync(runId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "In-process preparation failed for run {RunId}", runId);
            }
        });
        return Task.CompletedTask;
    }
}
