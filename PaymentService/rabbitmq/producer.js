const { connectRabbitMQ } = require("./connections");

const sendToQueue = async (queueName, message) => {
  const channel = await connectRabbitMQ();

  await channel.assertQueue(queueName, {
    durable: true
  });

  channel.sendToQueue(
    queueName,
    Buffer.from(JSON.stringify(message)),
    {
      persistent: true
    }
  );

  console.log(`Message sent to ${queueName}:`, message);
};

module.exports = {
  sendToQueue
};