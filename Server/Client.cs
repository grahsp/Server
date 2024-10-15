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

        //Settings fields
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

        public Client(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 4)
                throw new ArgumentException("ID must not be null, empty, or less than 4 characters.", nameof(id)); //Add better validation

            ID = id;
        }

        public async Task ConnectAsync(string ip, int port) //Add overloads for different types of connections
        {
            _client = new TcpClient();
            try
            {
                var connectTask = _client.ConnectAsync(ip, port);
                // Connect with a timeout
                if (await Task.WhenAny(connectTask, Task.Delay(_connectionTimeout)) != connectTask)
                    throw new TimeoutException($"Connection to {ip}:{port} timed out.");

                ClientStream = _client.GetStream();
                SetLastActive();

                RaiseOnConnect();
            }
            catch (SocketException ex)
            {
                _client = null;
                throw new InvalidOperationException($"Failed to connect to {ip}:{port}.", ex);
            }
            catch (Exception ex)
            {
                _client = null;
                throw new InvalidOperationException("An unexpected error occurred while connecting.", ex);
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
            try
            {
                _client?.Close();
                ClientStream?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnection: {ex.Message}");
            }
            finally
            {
                _client?.Dispose();
                ClientStream?.Dispose();

                _client = null;
                ClientStream = null;

                RaiseOnDisconnect();
            }
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
