namespace Server.Interfaces
{
    public interface INetworkStream
    {
        Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);
    }
}
