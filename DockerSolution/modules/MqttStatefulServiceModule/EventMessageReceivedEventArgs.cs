namespace MqttStatefulServiceModule
{
    public class EventMessageReceivedEventArgs : EventArgs
    {
        public string? Body { get; set; }

        public string? MyProperty { get; set; }
    }
}
