require("dotenv").config();

const express = require("express");
const bodyParser = require("body-parser");

const paymentRoutes = require("./routes/paymentRoutes");
const { startConsumers } = require("./rabbitmq/consumer");

const app = express();
const PORT = process.env.PORT || 3000;

app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(express.static("public"));

app.use("/api/payment", paymentRoutes);

app.listen(PORT, async () => {
  console.log(`Server is running on port ${PORT}`);

  try {
    await startConsumers();
  } catch (error) {
    console.error("Failed to start RabbitMQ consumers:", error.message);
  }
});