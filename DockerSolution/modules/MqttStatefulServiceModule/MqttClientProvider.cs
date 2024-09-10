using MQTTnet.Client;
using MQTTnet;
using System.Security.Cryptography.X509Certificates;

namespace MqttStatefulServiceModule;

public class  MqttClientProvider
{
    private ILogger _logger;

    private IMqttClient mqttClient = null;

    private MqttFactory mqttFactory = null;

    private string _publishTopic = null;

    public MqttClientProvider(string brokerHostName, int brokerPort, string deviceId, string publishTopic, ILogger logger)
    {
        _publishTopic = publishTopic;

        _logger = logger;

        // Add the loaded certificate to a certificate collection via files
        var pemCert = "client1-authnID.pem";
        var keyCert = "client1-authnID.key";
        var certificateCollection = new X509Certificate2Collection
        {
            new X509Certificate2(X509Certificate2.CreateFromPemFile(pemCert, keyCert).Export(X509ContentType.Pkcs12))
        };

        var tlsOptions =
                new MqttClientTlsOptionsBuilder()
                    .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12
                                        | System.Security.Authentication.SslProtocols.Tls13)
                    .WithClientCertificates(certificateCollection)
                    .Build();

        // Construct MQTT client
        mqttFactory = new MqttFactory();

        mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHostName, brokerPort)
            .WithClientId(deviceId)
            .WithCredentials(deviceId, "") // Password is not relevant for this scenario 
            .WithCleanSession(false) 
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithTlsOptions(tlsOptions)
            .Build();

        // Connect
        mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();
    }

    public bool PublishMessage(EventMessageReceivedEventArgs eventMessageRecieved)
    {
        var puback = mqttClient.PublishStringAsync(_publishTopic, eventMessageRecieved.Body).Result;

        _logger.LogInformation($"Event message {eventMessageRecieved.Body} published successfully ({puback.ReasonString}).");

        return string.IsNullOrEmpty(puback.ReasonString);
    }
}
