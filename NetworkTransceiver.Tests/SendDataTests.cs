using Moq;
using Transceiver = Server.Network.NetworkTransceiver;
using System.Text;
using Server.Interfaces;

namespace NetworkTransceiver.Tests
{
    [TestClass]
    public class SendDataTests
    {
        [TestMethod]
        public async Task SendDataAsync_SendsData_Successfully()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();

            var message = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            var messageLength = messageBytes.Length;
            var lengthHeader = BitConverter.GetBytes(messageLength);

            var expectedData = Transceiver.MergeBuffers(lengthHeader, messageBytes);
            var cancellationToken = new CancellationToken();

            // Setup the WriteAsync method of the mock to do nothing (simulating a successful send)
            mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            // Act
            await Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken);

            // Assert
            mockStream.Verify(s => s.WriteAsync(
                It.Is<byte[]>(buffer => buffer.SequenceEqual(expectedData)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SendDataAsync_HandlesLargeData_Successfully()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();

            var message = new string('a', 1000000);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            
            var messageLength = messageBytes.Length;
            var lengthHeader = BitConverter.GetBytes(messageLength);

            Console.WriteLine(messageLength);
            var expectedData = Transceiver.MergeBuffers(lengthHeader, messageBytes);
            var cancellationToken = new CancellationToken();

            // Setup the WriteAsync method of the mock to do nothing (simulating a successful send)
            mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            // Act
            await Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken);

            // Assert
            mockStream.Verify(s => s.WriteAsync(
                It.Is<byte[]>(buffer => buffer.SequenceEqual(expectedData)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}