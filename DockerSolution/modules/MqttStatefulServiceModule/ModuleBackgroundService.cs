namespace MqttStatefulServiceModule;

internal class ModuleBackgroundService : BackgroundService
{
    private int _counterSuccess = 0;
    
    private int _counterFail = 0;

    private CancellationToken _cancellationToken;
    
    private readonly ILogger<ModuleBackgroundService> _logger;

    private EventProcessorProvider _eventProcessorProvider;

    private MqttClientProvider _mqttClientProvider;

    /// <summary>
    /// CTOR
    /// </summary>
    /// <param name="logger"></param>
    public ModuleBackgroundService(ILogger<ModuleBackgroundService> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        string? eventHubNamespaceUri = Environment.GetEnvironmentVariable("eventHubNamespaceUri");
        string? consumerGroupName = Environment.GetEnvironmentVariable("consumerGroupName");
        string? eventHubName = Environment.GetEnvironmentVariable("eventHubName");
        string? blobStorageUri = Environment.GetEnvironmentVariable("blobStorageUri");

        string brokerHostName = Environment.GetEnvironmentVariable("brokerHostName");
        
        string brokerPortString = Environment.GetEnvironmentVariable("brokerPort");
        int brokerPort =
            !string.IsNullOrEmpty( brokerPortString)  
                ? int.Parse(brokerPortString)
                : 8883;

        string deviceId = Environment.GetEnvironmentVariable("deviceId");
        string publishTopic = Environment.GetEnvironmentVariable("publishTopic");

        _logger.LogInformation($"Environment variables expected: '{nameof(eventHubNamespaceUri)}', '{nameof(consumerGroupName)}', '{nameof(eventHubName)}', '{nameof(blobStorageUri)}', '{nameof(brokerHostName)}', '{nameof(brokerPort)}', '{nameof(deviceId)}', '{nameof(publishTopic)}'.");

        if (string.IsNullOrEmpty(eventHubNamespaceUri)
                ||  string.IsNullOrEmpty(consumerGroupName)
                || string.IsNullOrEmpty(eventHubName)
                || string.IsNullOrEmpty(blobStorageUri)
                || string.IsNullOrEmpty(brokerHostName)
                || string.IsNullOrEmpty(deviceId)
                || string.IsNullOrEmpty(publishTopic))
        {
            _logger.LogError($"Environment variables not found. Ignore providers.");
        }
        else
        {
            _logger.LogInformation($"Environment variables found: '{eventHubNamespaceUri}', '{consumerGroupName}', '{eventHubName}', '{blobStorageUri}'.");

            try
            {
                _mqttClientProvider = new MqttClientProvider(brokerHostName, brokerPort, deviceId, publishTopic, _logger);

                _logger.LogInformation($"MqttClientProvider initialized.");

                _eventProcessorProvider = new EventProcessorProvider(eventHubNamespaceUri!, consumerGroupName!, eventHubName!, blobStorageUri!, _logger);

                _eventProcessorProvider.EventMessageReceived += EventProcessorProvider_MessageReceived;

                _logger.LogInformation($"EventProcessorProvider initialized.");

                await _eventProcessorProvider.Start();

                _logger.LogInformation($"EventProcessorProvider started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing providers.");
            }
        }

        while (true)
        {
            _logger.LogInformation($"Processed: successful = {_counterSuccess} messages at {DateTime.UtcNow}");

            await Task.Delay(60000);
        }
    }

    private void EventProcessorProvider_MessageReceived(object? sender, EventMessageReceivedEventArgs e)
    {
        var success = _mqttClientProvider.PublishMessage(e);

        if (success)
        {
            _counterSuccess++;
        }
        else
        {
            _counterFail++;
        }
    }
}
