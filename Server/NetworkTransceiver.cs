using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class NetworkTransceiver
    {
        #region Sending Data
        public static async Task SendDataAsync(NetworkStream stream, string message)  //Replace stream with client wrapper
        {
            //Null checks and validation..

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            byte[] lengthBuffer = BitConverter.GetBytes(bytes.Length);

            byte[] data = MergeBuffers(lengthBuffer, bytes);    //Unecessary method?

            await stream.WriteAsync(data);
        }

        private static byte[] MergeBuffers(params byte[][] buffers)
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
        #endregion

        #region Receive Data
        private static async Task ReadBytes(NetworkStream stream, byte[] buffer, int messageLength)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < messageLength)
            {
                int bytesRead = await stream.ReadAsync(buffer);
                if (bytesRead <= 0)
                    throw new Exception("Connection closed before message was fully received.");

                totalBytesRead += bytesRead;
            }
        }
        #endregion
    }
}
