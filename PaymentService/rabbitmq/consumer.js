const { connectRabbitMQ } = require("./connections");

const startConsumers = async () => {
  const channel = await connectRabbitMQ();

  const queueName = "payment_status_queue";

  await channel.assertQueue(queueName, {
    durable: true
  });

  console.log(`Listening for messages on: ${queueName}`);

  channel.consume(queueName, async (msg) => {
    if (!msg) return;

    try {
      const data = JSON.parse(msg.content.toString());

      console.log("Received payment status message:", data);

      // Her kaldes notificationService, bookingService osv senere hen

      channel.ack(msg);
    } catch (error) {
      console.error("Consumer failed:", error.message);

      channel.nack(msg, false, false);
    }
  });
};

module.exports = {
  startConsumers
};