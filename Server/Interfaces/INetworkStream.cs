namespace Server.Interfaces
{
    public interface INetworkStream
    {
        Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);
        Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default);
        // Add other methods as needed
    }
}
