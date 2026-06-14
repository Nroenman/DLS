using Moq;
using Notification;
using Notification.Test;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;



namespace NotificationTest
{
    public class RabbitMqTest
    {
        [Fact]
        public async Task HandleMessage_SendsMail()
        {
            var fakeMail = new FakeMailSender();
            var rabbit = new RabbitMq(fakeMail);

            var message = new NotificationMessage
            {
                FromName = "Test",
                ToEmail = "test@test.dk",
                Subject = "Hej",
                Body = "Hello"
            };

            var json = JsonSerializer.Serialize(message);

            var ea = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(json)),
                DeliveryTag = 1
            };

            var channelMock = new Mock<IModel>();

            await rabbit.HandleMessageAsync(channelMock.Object, ea);

            Assert.Single(fakeMail.SentMessages);

            channelMock.Verify(x => x.BasicAck(1, false), Times.Once);
        }
        [Fact]
        public async Task HandleMessage_InvalidJson_CallsBasicNack()
        {
            var fakeMail = new FakeMailSender();
            var rabbit = new RabbitMq(fakeMail);

            var invalidJson = "{ this is not valid json }";

            var ea = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(invalidJson)),
                DeliveryTag = 1
            };

            var channelMock = new Mock<IModel>();

            await rabbit.HandleMessageAsync(channelMock.Object, ea);

            channelMock.Verify(
                x => x.BasicNack(1, false, false),
                Times.Once
            );

            channelMock.Verify(
                x => x.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()),
                Times.Never
            );

            Assert.Empty(fakeMail.SentMessages);
        }
        
    }
}
