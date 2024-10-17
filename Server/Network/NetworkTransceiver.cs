using Server.Interfaces;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server.Network
{
    public class NetworkTransceiver
    {
        private const int MaxPayloadSize = 1048576; // 1MB
        private static TimeSpan timeout = TimeSpan.FromSeconds(5);

        #region Sending Data
        public static async Task SendDataAsync(INetworkStream stream, string message, CancellationToken cancellationToken = default)  //Replace stream with client wrapper
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message), "Message cannot be null or empty.");

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            ValidatePayloadSize(bytes.Length);

            byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
            byte[] data = MergeBuffers(lengthBuffer, bytes);    //Unecessary method?

            // Create a race condition between the write operation and the timeout
            var writeTask = stream.WriteAsync(data, cancellationToken);
            var timeoutTask = Task.Delay(timeout);

            if (await Task.WhenAny(writeTask, timeoutTask) == timeoutTask)
                throw new TimeoutException("The send operation timed out.");

            await writeTask;
        }
        #endregion

        #region Receive Data
        public static async Task<string> ReceiveDataAsync(INetworkStream stream, CancellationToken cancellationToken = default)
        {
            byte[] lengthHeader = await ReadBytes(stream, 4, cancellationToken);

            int dataLength = BitConverter.ToInt32(lengthHeader);
            ValidatePayloadSize(dataLength);

            byte[] dataPayload = await ReadBytes(stream, dataLength, cancellationToken);

            return Encoding.UTF8.GetString(dataPayload);
        }

        private static async Task<byte[]> ReadBytes(INetworkStream stream, int expectedMessageLength, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Network stream cannot be null.");

            if (expectedMessageLength <= 0)
                return [];

            byte[] buffer = new byte[expectedMessageLength];
            int totalBytesRead = 0;
            while (totalBytesRead < expectedMessageLength)
            {
                // Read data into the buffer from the current offset
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, expectedMessageLength - totalBytesRead), cancellationToken);
                if (bytesRead <= 0)
                    throw new IOException("Connection closed unexpectedly.");

                totalBytesRead += bytesRead;
            }

            return buffer;
        }
        #endregion

        #region Utility
        public static byte[] MergeBuffers(params byte[][] buffers)
        {
            if (buffers == null)
                throw new ArgumentException(nameof(buffers), "buffers cannot be empty!");

            int totalLength = buffers.Sum(buffer => buffer?.Length ?? 0);
            byte[] result = new byte[totalLength];

            int offset = 0;
            foreach (var buffer in buffers)
            {
                if (buffer == null)
                    throw new ArgumentException("Atleast one buffer is set to null!", nameof(buffers));
                Buffer.BlockCopy(buffer, 0, result, offset, buffer.Length);
                offset += buffer.Length;
            }

            return result;
        }

        private static void ValidatePayloadSize(int dataSize)
        {
            if (dataSize <= 0 || dataSize > MaxPayloadSize)
                throw new InvalidOperationException($"Payload size must be positive and less than or equal to {MaxPayloadSize} bytes.");
        }
        #endregion
    }
}
