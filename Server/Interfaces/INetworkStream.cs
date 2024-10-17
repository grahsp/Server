namespace Server.Interfaces
{
    public interface INetworkStream : IDisposable
    {
        Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);
        Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
        void Close();
    }
}
