namespace MqttStatefulServiceModule;

public class  MqttClientProvider
{
    private ILogger _logger;

    public MqttClientProvider(ILogger logger)
    {
        _logger = logger;
    }

    public bool ProcessMessage(EventMessageReceivedEventArgs eventMessageRecieved)
    {
        // TODO

        _logger.LogInformation($"Event message {eventMessageRecieved.Body} processed successfully.");

        return true;
    }
}
