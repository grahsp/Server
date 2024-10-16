using Server.Interfaces;
using System.Net.Sockets;
using System.Text;

namespace Server.Network
{
    public class NetworkTransceiver
    {
        private const int MaxPayloadSize = 1048576; // 1MB

        #region Sending Data
        public static async Task SendDataAsync(INetworkStream stream, string message, CancellationToken cancellationToken = default)  //Replace stream with client wrapper
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message), "Message cannot be null or empty.");

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            ValidatePayloadSize(bytes.Length);

            byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
            byte[] data = MergeBuffers(lengthBuffer, bytes);    //Unecessary method?

            await stream.WriteAsync(data, cancellationToken);
        }
        #endregion

        #region Receive Data
        public static async Task<string> ReceiveDataAsync(NetworkStream stream)
        {
            byte[] lengthHeader = new byte[4];
            await ReadBytes(stream, lengthHeader);

            int dataLength = BitConverter.ToInt32(lengthHeader);
            ValidatePayloadSize(dataLength);

            byte[] dataPayload = new byte[dataLength];
            await ReadBytes(stream, dataPayload);

            return Encoding.UTF8.GetString(dataPayload);
        }

        private static async Task ReadBytes(NetworkStream stream, byte[] dataBuffer)
        {
            await ReadBytes(stream, dataBuffer, dataBuffer.Length);
        }

        private static async Task ReadBytes(NetworkStream stream, byte[] dataBuffer, int expectedMessageLength, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Network stream cannot be null.");

            if (dataBuffer == null)
                throw new ArgumentNullException(nameof(dataBuffer), "Buffer cannot be null.");

            if (expectedMessageLength <= 0 || expectedMessageLength > dataBuffer.Length)
                throw new ArgumentOutOfRangeException(nameof(expectedMessageLength), "Message length must be positive and less than or equal to buffer size.");


            int totalBytesRead = 0;
            while (totalBytesRead < expectedMessageLength)
            {
                // Read data into the buffer from the current offset
                int bytesRead = await stream.ReadAsync(dataBuffer);
                if (bytesRead <= 0)
                    throw new IOException("Connection closed before message was fully received.");

                totalBytesRead += bytesRead;
            }
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
            if (dataSize < 0 || dataSize > MaxPayloadSize)
                throw new ArgumentOutOfRangeException(nameof(dataSize), $"Data size must be positive and less than or equal to {MaxPayloadSize} bytes.");
        }
        #endregion
    }
}
