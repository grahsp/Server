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
        public async Task SendDataAsync_MultipleMessages_SentSuccessfully()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var messages = new[] { "Message 1", "Message 2", "Message 3" };
            var cancellationToken = new CancellationToken();

            // Setup the WriteAsync method of the mock to do nothing (simulating a successful send)
            mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            // Act
            var tasks = messages.Select(message => Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken));
            await Task.WhenAll(tasks);

            // Assert
            foreach (var message in messages)
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                var messageLength = messageBytes.Length;
                var lengthHeader = BitConverter.GetBytes(messageLength);
                var expectedData = Transceiver.MergeBuffers(lengthHeader, messageBytes);

                mockStream.Verify(s => s.WriteAsync(
                    It.Is<byte[]>(buffer => buffer.SequenceEqual(expectedData)),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
            }
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
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SendDataAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            string message = null!;
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

        [TestMethod]
        public async Task SendDataAsync_RespectsCancellationToken()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var message = "Hello, World!";
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Setup the WriteAsync method of the mock to delay and then throw a TaskCanceledException
            mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .Returns(async (byte[] buffer, CancellationToken ct) =>
                      {
                          await Task.Delay(1000, ct);
                          ct.ThrowIfCancellationRequested();
                      });

            // Act
            cancellationTokenSource.Cancel();
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
                Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken));
        }

        [TestMethod]
        public async Task SendDataAsync_StreamThrowsException_HandlesCorrectly()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var message = "Hello, World!";
            var cancellationToken = new CancellationToken();

            // Setup the WriteAsync method of the mock to throw an IOException
            mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new IOException("Stream error"));

            // Act & Assert
            await Assert.ThrowsExceptionAsync<IOException>(() =>
                Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken));
        }

        [TestClass]
        public class NetworkTransceiverTests
        {
            [TestMethod]
            public async Task SendDataAsync_WriteTimeout_ThrowsTimeoutException()
            {
                // Arrange
                var mockStream = new Mock<INetworkStream>();
                var message = "Hello, World!";
                var cancellationToken = new CancellationToken();

                // Setup the WriteAsync method to delay indefinitely
                mockStream.Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                          .Returns(async (byte[] buffer, CancellationToken ct) =>
                          {
                              await Task.Delay(Timeout.Infinite, ct);
                          });

                // Act & Assert
                await Assert.ThrowsExceptionAsync<TimeoutException>(() =>
                    Transceiver.SendDataAsync(mockStream.Object, message, cancellationToken));
            }
        }
    }
}