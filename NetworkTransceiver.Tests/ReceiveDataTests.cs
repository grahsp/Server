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
            var sequence = new Queue<byte[]>(new[] { lengthBytes, messageBytes });
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                        {
                            var data = sequence.Dequeue();
                            data.CopyTo(buffer);
                            return data.Length;
                        });

            // Act
            var result = await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert
            Assert.AreEqual(message, result);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task ReceiveDataAsync_InvalidLengthHeader_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var lengthBytes = BitConverter.GetBytes(-1);

            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                        {
                            lengthBytes.CopyTo(buffer);
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
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .Returns((Memory<byte> buffer, CancellationToken ct) =>
                        {
                            if (callCount == 0)
                            {
                                lengthBytes.CopyTo(buffer);
                                callCount++;
                                return Task.FromResult(lengthBytes.Length);
                            }
                            else if (callCount == 1)
                            {
                                partialMessageBytes.CopyTo(buffer);
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task ReceiveDataAsync_NullStream_ThrowsArgumentNullException()
        {
            // Act
            await Transceiver.ReceiveDataAsync(null!);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        public async Task ReceiveDataAsync_LargePayload_ReturnsMessage()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var message = new string('A', 1048576); // 1MB message
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            var sequence = new Queue<byte[]>(new[] { lengthBytes, messageBytes });
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                        {
                            var data = sequence.Dequeue();
                            data.CopyTo(buffer);
                            return data.Length;
                        });

            // Act
            var result = await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert
            Assert.AreEqual(message, result);
        }

        [TestMethod]
        public async Task ReceiveDataAsync_PartialReads_ReturnsMessage()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>(MockBehavior.Loose);
            var message = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            var sequence = new Queue<byte[]>(new[] { lengthBytes, messageBytes.Take(5).ToArray(), messageBytes.Skip(5).ToArray() });
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                        {
                            var data = sequence.Dequeue();
                            data.CopyTo(buffer);
                            return data.Length;
                        });

            // Act
            var result = await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert
            Assert.AreEqual(message, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task ReceiveDataAsync_EmptyMessage_ThrowsInvalidOperationException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var lengthBytes = BitConverter.GetBytes(0);

            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                      {
                          lengthBytes.CopyTo(buffer);
                          return lengthBytes.Length;
                      });

            // Act
            await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert is handled by ExpectedException
        }

        [TestMethod]
        public async Task ReceiveDataAsync_PartialLengthHeader_ReturnsMessage()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var message = "Hello, World!";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            var sequence = new Queue<byte[]>(new[] { lengthBytes.Take(2).ToArray(), lengthBytes.Skip(2).ToArray(), messageBytes });
            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                      {
                          var data = sequence.Dequeue();
                          data.CopyTo(buffer);
                          return data.Length;
                      });

            // Act
            var result = await Transceiver.ReceiveDataAsync(mockStream.Object);

            // Assert
            Assert.AreEqual(message, result);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public async Task ReceiveDataAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var mockStream = new Mock<INetworkStream>();
            var lengthBytes = BitConverter.GetBytes(4);

            mockStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Memory<byte> buffer, CancellationToken ct) =>
                      {
                          ct.ThrowIfCancellationRequested();
                          lengthBytes.CopyTo(buffer);
                          return lengthBytes.Length;
                      });

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            await Transceiver.ReceiveDataAsync(mockStream.Object, cts.Token);

            // Assert is handled by ExpectedException
        }
    }
}