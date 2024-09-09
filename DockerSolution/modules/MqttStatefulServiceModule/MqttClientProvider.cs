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

        // Add the loaded certificate to a certificate collection
        X509Certificate2Collection certificateCollection = new X509Certificate2Collection
        {
            new X509Certificate2(X509Certificate2.CreateFromPem(pemCert, keyCert).Export(X509ContentType.Pkcs12))
        };

        var tlsOptions =
                new MqttClientTlsOptionsBuilder()
                    .WithSslProtocols(System.Security.Authentication.SslProtocols.Tls12
                                        | System.Security.Authentication.SslProtocols.Tls13)
                    .WithClientCertificates(certificateCollection)
                    .Build();

        // connect
        mqttFactory = new MqttFactory();

        mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHostName, brokerPort)
            .WithClientId(deviceId)
            .WithCredentials(deviceId, "")  //use client authentication name in the username
            .WithCleanSession(false) 
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithTlsOptions(tlsOptions)
            .Build();

        // Connect
        mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();
    }

    public bool ProcessMessage(EventMessageReceivedEventArgs eventMessageRecieved)
    {
        var puback = mqttClient.PublishStringAsync(_publishTopic, eventMessageRecieved.Body).Result;

        _logger.LogInformation($"Event message {eventMessageRecieved.Body} published successfully ({puback.ReasonString}).");

        return true;
    }

    private static string keyCert = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIKqxcNh6yYJPUrtHR6hIOs6q3I+2VWGn+8BM8c/paT6WoAoGCCqGSM49
AwEHoUQDQgAE4lTqBbb62dCyL5UEPbQDEjGi5YfMN0RNtBh+P6RMQ0bFIAf+BPZC
JFHkaVZ6matbH9tBu+cQNV223DWDppM6vA==
-----END EC PRIVATE KEY-----";

    private static string pemCert = @"-----BEGIN CERTIFICATE-----
MIIB3zCCAYWgAwIBAgIQTgzW3MOcMQvK1Cq4Z/wrWTAKBggqhkjOPQQDAjA4MRIw
EAYDVQQKEwlBY2lUZXN0Q0ExIjAgBgNVBAMTGUFjaVRlc3RDQSBJbnRlcm1lZGlh
dGUgQ0EwHhcNMjQwOTAyMTQzMDI2WhcNMzIxMTE5MTQzMDEwWjAaMRgwFgYDVQQD
Ew9jbGllbnQxLWF1dGhuSUQwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAATiVOoF
tvrZ0LIvlQQ9tAMSMaLlh8w3RE20GH4/pExDRsUgB/4E9kIkUeRpVnqZq1sf20G7
5xA1XbbcNYOmkzq8o4GOMIGLMA4GA1UdDwEB/wQEAwIHgDAdBgNVHSUEFjAUBggr
BgEFBQcDAQYIKwYBBQUHAwIwHQYDVR0OBBYEFAsfqnUYFdC8pM0AHMB8Y1hr+1bK
MB8GA1UdIwQYMBaAFC4tuqtJumyiyaO0pwMkgFtrtXf+MBoGA1UdEQQTMBGCD2Ns
aWVudDEtYXV0aG5JRDAKBggqhkjOPQQDAgNIADBFAiEAhHi/OxM+W8LVg6EMTwb6
hQSr3NJNQJQYqbG+hS0hyEsCIFSaIJ+kSoKWPj9Sbs0pcwnkBLXWgat/GE6YxG8C
Evyw
-----END CERTIFICATE-----";

}
