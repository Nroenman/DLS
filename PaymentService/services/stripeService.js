require("dotenv").config();

const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const { sendToQueue } = require("../rabbitmq/producer");

const sendNotification = async (paymentStatus) => {
  await sendToQueue("payment_status_queue", paymentStatus);
};

const stripeCheckout = async (req, res) => {
  const flightID = req.body.flightID;

  try {
    const session = await stripe.checkout.sessions.create({
      mode: "payment",
      line_items: [
        {
          price_data: {
            currency: "DKK",
            product_data: {
              name: "Testprodukt"
            },
            unit_amount: 5000
          },
          quantity: 1
        }
      ],
      success_url: `http://localhost:3000/api/payment/stripe/success/${flightID}`,
      cancel_url: `http://localhost:3000/api/payment/stripe/cancel/${flightID}`
    });

    res.status(200).json({ url: session.url });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
};

const stripePayment = async (req, res) => {
  try {
    const paymentIntent = await stripe.paymentIntents.create({
      amount: 5000,
      currency: "DKK",
      automatic_payment_methods: {
        enabled: true
      }
    });

    res.status(200).json(paymentIntent);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
};

const successRedirect = async (req, res) => {
  const paymentStatus = {
    flightID: req.params.flightID,
    isPaid: true,
    status: "PAYMENT_SUCCESS"
  };

  try {
    await sendNotification(paymentStatus);

    res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Failed to send RabbitMQ notification:", error.message);

    res.status(500).json({
      error: error.message
    });
  }
};

const cancelRedirect = async (req, res) => {
  const paymentStatus = {
    flightID: req.params.flightID,
    isPaid: false,
    status: "PAYMENT_CANCELLED"
  };

  try {
    await sendNotification(paymentStatus);

    res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Failed to send RabbitMQ notification:", error.message);

    res.status(500).json({
      error: error.message
    });
  }
};

module.exports = {
  stripePayment,
  stripeCheckout,
  successRedirect,
  cancelRedirect
};