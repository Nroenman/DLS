require("dotenv").config();
const express = require("express");
const paymentRoutes = require("./routes/paymentRoutes");
const webhookRoutes = require("./routes/webhookRoutes");
const { startConsumers } = require("./rabbitmq/consumer");
const app = express();
const PORT = process.env.PORT || 3000;

app.use(
  "/api/payment/stripe/webhook",
  express.raw({ type: "application/json" }),
  webhookRoutes
);
app.use(express.json());
app.use(express.urlencoded({ extended: true }));
app.use(express.static("public"));
app.use("/api/payment", paymentRoutes);

app.listen(PORT, async () => {
  console.log(`Server is running on port ${PORT}`);

  try {
    await startConsumers();
  } catch (error) {
    console.error(
      "Failed to start RabbitMQ consumers:",
      error.message
    );
  }
});