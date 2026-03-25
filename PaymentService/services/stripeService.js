require("dotenv").config();

const Payment = require("../models/Payment");
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const { sendToQueue } = require("../rabbitmq/producer");

const sendNotification = async (notification) => {
  await sendToQueue("Notification", notification);
};

const stripeCheckout = async (req, res) => {
  console.log("Received checkout request with body:", req.body);

  const {
    BookingId,
    UserId,
    TotalPrice,
    ContactEmail
  } = req.body;

  const booking_id = BookingId;
  const user_id = UserId;
  const amount = Math.round(Number(TotalPrice) * 100);
  const currency = "DKK";
  const userEmail = ContactEmail;

  if (!booking_id || !user_id || !TotalPrice || Number.isNaN(amount)) {
    return res.status(400).json({
      error: "BookingId, UserId and TotalPrice are required"
    });
  }

  const idempotencyKey = `checkout-${booking_id}`;

  try {
    const existingPayment = await Payment.findOne({
      where: {
        booking_id,
        status: "PENDING"
      }
    });

    if (existingPayment && existingPayment.stripe_session_id) {
      console.log("Existing pending payment found:", existingPayment.toJSON());

      const existingSession = await stripe.checkout.sessions.retrieve(
        existingPayment.stripe_session_id
      );

      console.log("Retrieved existing session:", existingSession.id);

      if (existingSession.url == null) {
        return res.status(400).json({
          error: "Existing payment session has expired. Please try again."
        });
      }

      return res.status(200).json({
        url: existingSession.url,
        reused: true
      });
    }

    const session = await stripe.checkout.sessions.create(
      {
        mode: "payment",
        client_reference_id: String(booking_id),

        line_items: [
          {
            price_data: {
              currency: currency.toLowerCase(),
              product_data: {
                name: `Booking ${booking_id}`
              },
              unit_amount: amount
            },
            quantity: 1
          }
        ],

        success_url: `http://localhost:3001/api/payment/stripe/success?session_id={CHECKOUT_SESSION_ID}`,
        cancel_url: `http://localhost:3001/api/payment/stripe/cancel?booking_id=${booking_id}`,

        metadata: {
          booking_id: String(booking_id),
          user_id: String(user_id),
          userEmail: userEmail || ""
        }
      },
      {
        idempotencyKey
      }
    );

    const existingByBooking = await Payment.findOne({
      where: { booking_id }
    });

    if (existingByBooking) {
      await existingByBooking.update({
        user_id,
        idempotency_key: idempotencyKey,
        amount,
        currency,
        status: "PENDING",
        stripe_session_id: session.id
      });
    } else {
      await Payment.create({
        booking_id,
        user_id,
        idempotency_key: idempotencyKey,
        amount,
        currency,
        status: "PENDING",
        stripe_session_id: session.id
      });
    }

    return res.status(200).json({
      url: session.url,
      reused: false
    });
  } catch (error) {
    console.error("Stripe checkout error:", error.message);

    return res.status(500).json({
      error: error.message
    });
  }
};

const successRedirect = async (req, res) => {
  const sessionId = req.query.session_id;

  if (!sessionId) {
    return res.status(400).json({
      error: "session_id is required"
    });
  }

  try {
    const session = await stripe.checkout.sessions.retrieve(sessionId);

    const email = session.metadata.userEmail;
    const booking_id = session.metadata.booking_id;

    const paymentStatus = {
      booking_id,
      isPaid: true,
      status: "COMPLETED"
    };

    const notificationMessage = {
      fromName: "Airport Payment Service",
      toEmail: email,
      subject: "Payment successful",
      body: `Payment for booking ${booking_id} was successful.`
    };

    await Payment.update(
      {
        status: paymentStatus.status
      },
      {
        where: { booking_id }
      }
    );

    await sendNotification(notificationMessage);

    return res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Payment success handling failed:", error.message);

    return res.status(500).json({
      error: error.message
    });
  }
};

const cancelRedirect = async (req, res) => {
  const booking_id = req.query.booking_id;

  if (!booking_id) {
    return res.status(400).json({
      error: "booking_id is required"
    });
  }

  const paymentStatus = {
    booking_id,
    isPaid: false,
    status: "PENDING"
  };

  const notificationMessage = {
    fromName: "Airport Payment Service",
    toEmail: "",
    subject: "Payment not completed",
    body: `Payment for booking ${booking_id} was not successful.`
  };

  try {
    await Payment.update(
      {
        status: paymentStatus.status
      },
      {
        where: { booking_id }
      }
    );

    await sendNotification(notificationMessage);

    return res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Payment cancel handling failed:", error.message);

    return res.status(500).json({
      error: error.message
    });
  }
};

const getPaymentsByUserId = async (req, res) => {
  const { userId } = req.params;

  try {
    const payments = await Payment.findAll({
      where: {
        user_id: userId
      },
      attributes: [
        "id",
        "booking_id",
        "user_id",
        "amount",
        "currency",
        "status",
        "stripe_session_id",
        "created_at",
        "updated_at"
      ],
      order: [["created_at", "DESC"]]
    });

    return res.status(200).json(payments);
  } catch (error) {
    console.error("Failed to get payments by user:", error.message);

    return res.status(500).json({
      error: error.message
    });
  }
};

const getPaymentByBookingId = async (req, res) => {
  const { bookingId } = req.params;

  try {
    const payment = await Payment.findOne({
      where: {
        booking_id: bookingId
      },
      attributes: [
        "id",
        "booking_id",
        "user_id",
        "amount",
        "currency",
        "status",
        "stripe_session_id",
        "created_at",
        "updated_at"
      ],
      order: [["created_at", "DESC"]]
    });

    if (!payment) {
      return res.status(404).json({
        error: "Payment not found"
      });
    }

    return res.status(200).json(payment);
  } catch (error) {
    console.error("Failed to get payment by booking:", error.message);

    return res.status(500).json({
      error: error.message
    });
  }
};

module.exports = {
  stripeCheckout,
  successRedirect,
  cancelRedirect,
  getPaymentsByUserId,
  getPaymentByBookingId
};
