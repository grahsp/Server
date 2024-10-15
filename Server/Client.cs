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

        public NetworkStream? ClientStream;
        public bool IsConnected { get => _client?.Connected ?? false; }

        private TcpClient? _client;
        private readonly CancellationTokenSource _cts = new();


        public Client(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 4) throw new Exception("Invalid ID"); //Add better validation

            ID = id;
        }

        public async Task ConnectAsync(string ip, int port) //Add overloads for different types of connections
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(ip, port);
                if (IsConnected) throw new Exception("Failed to connect to the server.");
                ClientStream = _client.GetStream();
                SetLastActive();

                RaiseOnConnect();
            }
            catch (Exception ex)
            {
                _client = null;
                throw new InvalidOperationException("Failed to connect to the server.", ex);
            }
        }

        public async Task SendAsync(string data)
        {
            // Placeholder
            SetLastActive();
        }

        public async Task ListenAsync(CancellationToken token)
        {
            // Placeholder
        }

        private void SetLastActive()
        {
            LastActive = DateTime.Now.TimeOfDay;
        }

        private void RaiseOnConnect()
        {
            OnConnect?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseOnDisconnect()
        {
            OnDisconnect?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseOnDataReceived(string data)
        {
            OnDataReceived?.Invoke(this, data);
        }

        public void Disconnect()
        {
            _client?.Close();
            _client?.Dispose();
            ClientStream?.Dispose();

            _client = null;
            ClientStream = null;
            
            RaiseOnDisconnect();
        }

        public void Dispose()
        {
            _cts.Cancel();
            Disconnect();

            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
