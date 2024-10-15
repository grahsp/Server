using System.Net.Sockets;

namespace Server
{
    public class Client : IDisposable
    {
        // Events
        public EventHandler? OnConnect;
        public EventHandler? OnDisconnect;
        public EventHandler<string>? OnDataReceived;

        public string ID { get; }
        public TimeSpan LastActive { get; private set; }

        public NetworkStream? Stream;
        public bool IsConnected { get => _client.Connected; }

        private TcpClient _client;
        private CancellationTokenSource _cts = new();


        public Client(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) || id.Length < 4) throw new Exception("Invalid ID");

            ID = id;
            _client = new TcpClient();
        }

        public async Task ConnectAsync(string ip, int port) //Add overloads for different types of connections
        {
            try
            {
                await _client.ConnectAsync(ip, port);
                if (!_client.Connected) throw new Exception("Failed to connect to the server.");
                Stream = _client.GetStream();
                SetLastActive();

                OnConnect?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _client = null!;
                throw new InvalidOperationException("Failed to connect to the server.", ex);
            }
        }

        public void Disconnect()
        {
            _client.Close();
            _client.Dispose();
            _client = null!;
            OnDisconnect?.Invoke(this, EventArgs.Empty);

        }

        private void SetLastActive()
        {
            LastActive = DateTime.Now.TimeOfDay;
        }

        public void Dispose()
        {
            _cts.Cancel();
            Disconnect();

            _cts.Dispose();
        }
    }
}
