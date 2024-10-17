using Server.Interfaces;
using Server.Network;
using System.Net.Sockets;

namespace Server
{
    public class Client : IDisposable
    {
        // Events
        public EventHandler? OnConnect;
        public EventHandler? OnDisconnect;
        public EventHandler<string>? OnDataReceived;
        public EventHandler? OnDataSent;

        public string ID { get; }
        public TimeSpan LastActive { get; private set; }

        public INetworkStream? ClientStream;
        public bool IsConnected { get => _client?.Connected ?? false; }

        private TcpClient? _client;
        private readonly CancellationTokenSource _cts = new();

        // Settings fields
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

        public Client(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length < 4)
                throw new ArgumentException("ID must not be null, empty, or less than 4 characters.", nameof(id)); //Add better validation

            ID = id;
        }

        public async Task ConnectAsync(string ip, int port) // Add overloads for different types of connections
        {
            _client = new TcpClient();
            try
            {
                var connectTask = _client.ConnectAsync(ip, port);
                // Connect with a timeout
                if (await Task.WhenAny(connectTask, Task.Delay(_connectionTimeout)) != connectTask)
                    throw new TimeoutException($"Connection to {ip}:{port} timed out.");

                var stream = _client.GetStream();
                ClientStream = new NetworkStreamWrapper(stream);
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
            if (!IsConnected) throw new InvalidOperationException("TcpClient is not connected!");
            if (ClientStream == null) throw new InvalidOperationException("Client stream is not initialized!");

            try
            {
                await NetworkTransceiver.SendDataAsync(ClientStream, data);
                SetLastActive();

                RaiseOnDataSent();
            }
            catch(Exception ex)
            {
                Disconnect();
                throw new InvalidOperationException("Failed to send data to the client.", ex);
            }
        }

        public async Task ListenAsync(CancellationToken token)
        {
            if (!IsConnected) throw new InvalidOperationException("TcpClient is not connected!");
            if (ClientStream == null) throw new InvalidOperationException("Client stream is not initialized!");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var data = await NetworkTransceiver.ReceiveDataAsync(ClientStream);
                    if (string.IsNullOrEmpty(data)) continue;

                    OnDataReceived?.Invoke(this, data);
                    await Task.Delay(50); // Add a delay to prevent high CPU usage
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
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

        private void RaiseOnDataSent()
        {
            OnDataSent?.Invoke(this, EventArgs.Empty);
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
