const amqp = require("amqplib");

let connection;
let channel;

const connectRabbitMQ = async () => {
  if (channel) {
    return channel;
  }

  const rabbitUrl = process.env.RABBITMQ_URL;

  connection = await amqp.connect(rabbitUrl);
  channel = await connection.createChannel();

  console.log("Connected to RabbitMQ");

  return channel;
};

module.exports = {
  connectRabbitMQ
};