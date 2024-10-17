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

        public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            await _networkStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        public async Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            return await _networkStream.ReadAsync(buffer, cancellationToken);
        }
    }
}
