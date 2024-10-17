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
    }
}
