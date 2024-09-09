namespace MqttStatefulServiceModule
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string? Body { get; set; }
    }
}
