using Moq;
using Server.Interfaces;
using System.Text;
using Transceiver = Server.Network.NetworkTransceiver;

namespace NetworkTransceiver.Tests
{
    [TestClass]
    public class ReceiveDataTests
    {
        [TestMethod]
        public async Task ReceiveDataAsync_ValidData_ReturnsMessage()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var message = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            // Setup the ReadAsync method to return the length and then the message
            var sequence = new Queue<byte[]>([lengthBytes, messageBytes]);
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[] buffer, CancellationToken ct) =>
                      {
                          var data = sequence.Dequeue();
                          Array.Copy(data, 0, buffer, 0, data.Length);
                          return data.Length;
                      });

            // Act
            var result = await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert
            Assert.AreEqual(message, result);
        }

        // ADD TEST FOR EMPTY MESSAGE THAT SHOULD THROW EXCEPTION AND DISCONNECT CLIENT

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task ReceiveDataAsync_InvalidLengthHeader_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var lengthBytes = BitConverter.GetBytes(-1);

            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[] buffer, CancellationToken ct) =>
                      {
                          Array.Copy(lengthBytes, 0, buffer, 0, lengthBytes.Length);
                          return lengthBytes.Length;
                      });

            // Act
            await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public async Task ReceiveDataAsync_StreamClosedBeforeFullRead_ThrowsIOException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var lengthBytes = BitConverter.GetBytes(4);
            var partialMessageBytes = new byte[] { 1, 2 };

            var callCount = 0;
            mockStream.Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .Returns((byte[] buffer, CancellationToken ct) =>
                      {
                          if (callCount == 0)
                          {
                              Array.Copy(lengthBytes, 0, buffer, 0, lengthBytes.Length);
                              callCount++;
                              return Task.FromResult(lengthBytes.Length);
                          }
                          else if (callCount == 1)
                          {
                              Array.Copy(partialMessageBytes, 0, buffer, 0, partialMessageBytes.Length);
                              callCount++;
                              return Task.FromResult(partialMessageBytes.Length);
                          }
                          else
                          {
                              return Task.FromResult(0); // Simulate stream closed
                          }
                      });

            // Act
            await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert is handled by ExpectedException
        }
    }
}
