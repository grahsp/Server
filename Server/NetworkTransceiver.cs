namespace Server
{
    internal class NetworkTransceiver
    {
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
    }
}
