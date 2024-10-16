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
        public async Task SendDataAsync_HandleLargeData_Successfully()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();

            var message = new string('a', 1000000);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendDataAsync_EmptyMessage_ThrowArgumentNullException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();

            var message = string.Empty;
            var cancellationToken = new CancellationToken();

            // Act
            await Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task SendDataAsync_HandleTooLargeData_ThrowArgumentOutOfRangeException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();

            var message = new string('a', 1048577); // 1MB + 1B

            var cancellationToken = new CancellationToken();

            // Act
            await Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken);

            // Assert is handled by ExpectedException
        }
    }
}