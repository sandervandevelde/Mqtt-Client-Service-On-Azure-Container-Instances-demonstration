
using System.Text;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;

namespace MqttStatefulServiceModule;

public class EventProcessorProvider
{
    private ILogger _logger;

    private EventProcessorClient? _eventProcessorClient;

    public EventProcessorProvider(string eventHubNamespaceUri, string consumerGroupName, string eventHubName, string blobStorageUri, ILogger logger)
    {
        _logger = logger;

        var storageClient = new BlobContainerClient(new Uri(blobStorageUri), new DefaultAzureCredential());

        _logger.LogInformation("BlobContainerClient created");

        // Create an event processor client to process events in the event hub

        _eventProcessorClient = 
            new EventProcessorClient(
                storageClient, 
                consumerGroupName, 
                eventHubNamespaceUri, 
                eventHubName, 
                new DefaultAzureCredential(), 
                new EventProcessorClientOptions 
                { 
                    PartitionOwnershipExpirationInterval = TimeSpan.FromSeconds(10),
                    LoadBalancingUpdateInterval = TimeSpan.FromSeconds(5),
                });

        _logger.LogInformation("EventProcessorClient created");

        // Register handlers for processing events and handling errors
        _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
        _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;

        _logger.LogInformation("EventProcessorClient event handlers registered");
    }

    public async Task Start()
    { 
        // Start the processing
        await _eventProcessorClient!.StartProcessingAsync();

        _logger.LogInformation($"Processing started.");
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("ProcessEventArgs Cancellation requested.");

                return;
            }

            var body = Encoding.UTF8.GetString(eventArgs!.Data!.Body!.ToArray());

            _logger.LogInformation($"EventHub event processed: '{body}'");

            var messageReceivedEventArgs = new EventMessageReceivedEventArgs { Body = body };

            OnEventMessageReceived(messageReceivedEventArgs);

            await eventArgs.UpdateCheckpointAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProcessEventArgs");
        }
    }

    private async Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("ProcessErrorEventArgs Cancellation requested.");

                return;
            }

            var errorMessage = $"EventHub error received: '{eventArgs.Exception}' ({eventArgs.Operation ?? "Unknown"}).";
            
            _logger.LogInformation(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ProcessErrorEventArgs");
        }
    }

    protected virtual void OnEventMessageReceived(EventMessageReceivedEventArgs e)
    {
        EventHandler<EventMessageReceivedEventArgs> handler = EventMessageReceived;

        if (handler != null)
        {
            handler(this, e);
        }
    }

    public event EventHandler<EventMessageReceivedEventArgs> EventMessageReceived;
}
