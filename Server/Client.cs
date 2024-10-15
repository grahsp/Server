using System.Net.Sockets;

namespace Server
{
    public class Client
    {
        // Events
        public EventHandler? OnConnect;
        public EventHandler? OnDisconnect;
        public EventHandler<string>? OnDataReceived;

        public string ID { get; }
        public DateTime LastActive { get; private set; }

        public NetworkStream Stream;
        public bool IsConnected { get => _client.Connected; }

        private TcpClient _client;
        private CancellationTokenSource _cts = new();


        public Client(TcpClient client)
        {
            _client = client;
        }
    }
}
