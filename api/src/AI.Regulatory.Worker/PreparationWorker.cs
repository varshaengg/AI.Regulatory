using System.Text.Json;
using AI.Regulatory.API.Contracts;
using AI.Regulatory.API.Data;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

namespace AI.Regulatory.Worker;

/// <summary>Consumes run commands and executes the preparation stages for each run.</summary>
public sealed class PreparationWorker : BackgroundService
{
    private const int MaxDeliveries = 5; // keep in step with the queue's maxDeliveryCount

    private readonly PreparationRunService _service;
    private readonly PreparationRunsRepository _runs;
    private readonly IConfiguration _config;
    private readonly ILogger<PreparationWorker> _log;

    public PreparationWorker(
        PreparationRunService service,
        PreparationRunsRepository runs,
        IConfiguration config,
        ILogger<PreparationWorker> log)
    {
        _service = service;
        _runs = runs;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ns = _config["ServiceBus:FullyQualifiedNamespace"];
        if (string.IsNullOrWhiteSpace(ns))
        {
            _log.LogWarning("ServiceBus:FullyQualifiedNamespace is not configured; the worker is idle.");
            return;
        }

        var queue = _config["ServiceBus:DossierQueue"] ?? "dossier-runs";
        await using var client = new ServiceBusClient(ns, new DefaultAzureCredential());
        await using var processor = client.CreateProcessor(queue, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 2,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(30),
        });

        processor.ProcessMessageAsync += HandleAsync;
        processor.ProcessErrorAsync += args =>
        {
            _log.LogError(args.Exception, "Service Bus error on {Entity} ({Source})", args.EntityPath, args.ErrorSource);
            return Task.CompletedTask;
        };

        await processor.StartProcessingAsync(stoppingToken);
        _log.LogInformation("Preparation worker listening on queue {Queue}", queue);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }

        await processor.StopProcessingAsync(CancellationToken.None);
    }

    private async Task HandleAsync(ProcessMessageEventArgs args)
    {
        var message = JsonSerializer.Deserialize<PreparationRunMessage>(
            args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (message is null || string.IsNullOrWhiteSpace(message.RunId))
        {
            await args.DeadLetterMessageAsync(args.Message, "InvalidMessage", "Body is not a run command.", args.CancellationToken);
            return;
        }

        var run = await _runs.GetAsync(message.RunId, args.CancellationToken);
        if (run is null)
        {
            await args.DeadLetterMessageAsync(args.Message, "RunNotFound", message.RunId, args.CancellationToken);
            return;
        }

        // A redelivery after a completed run must not redo the work.
        if (run.Status == "Completed")
        {
            _log.LogInformation("Run {RunId} already completed; skipping duplicate message.", message.RunId);
            return;
        }

        try
        {
            await _service.DiscoverAsync(message.RunId, args.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Run {RunId} failed on delivery {Delivery}", message.RunId, args.Message.DeliveryCount);
            if (args.Message.DeliveryCount >= MaxDeliveries)
            {
                await _runs.AddEventsAsync(message.RunId,
                    [new PreparationRunEvent(DateTime.UtcNow, "Run", "Error", $"Processing failed after {MaxDeliveries} attempts ({ex.GetType().Name}).")],
                    CancellationToken.None);
                await _runs.SetStatusAsync(message.RunId, "Failed", CancellationToken.None);
            }
            throw; // abandon: Service Bus redelivers, then dead-letters
        }
    }
}
