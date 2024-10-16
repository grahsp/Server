using Server.Interfaces;
using System.Net.Sockets;

namespace Server.Network
{
    public class NetworkStreamWrapper : INetworkStream
    {
        private readonly NetworkStream _networkStream;

        public NetworkStreamWrapper(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            return _networkStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default)
        {
            return _networkStream.WriteAsync(buffer, offset, size, cancellationToken);
        }

        // Implement other methods as needed
    }
}
