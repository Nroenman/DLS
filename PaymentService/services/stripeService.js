require("dotenv").config();

const Payment = require("../models/Payment");
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const { sendToQueue } = require("../rabbitmq/producer");

const sendNotification = async (notification) => {
  await sendToQueue("Notification", notification);
};

const stripeCheckout = async (req, res) => {

const { BookingId, UserId, TotalPrice, ContactEmail, ContactPhone } = req.body;
const idempotency_key = req.headers['Idempotency-Key']; 
const booking_id = BookingId;
const user_id = UserId;
const amount = Math.round(Number(TotalPrice) * 100);
const currency = "DKK";
const userEmail = ContactEmail;
const userPhone = ContactPhone;

if (!booking_id || !user_id || !TotalPrice || Number.isNaN(amount) || !idempotency_key) {
  return res.status(400).json({
    error: "BookingId, UserId, TotalPrice and idempotencyKey are required"
  });
}

try {
  // Først tjek, om der allerede findes en betaling med samme idempotencyKey
  const existingPayment = await Payment.findOne({
    where: { idempotency_key }
  });
 console.log("Idempotency key " + idempotency_key);
  if (existingPayment) {
    if (existingPayment.status === "COMPLETED") {
       console.log("Idempotency key " + idempotency_key + " already exists. Status Completed.");
      return res.status(409).json({
        error: "Payment has already been completed for this request."
      });
    }

    if (existingPayment.status === "PENDING" && existingPayment.stripe_session_id) {
      console.log("Idempotency key " + idempotency_key + " already exists. Status pending.");
      const existingSession = await stripe.checkout.sessions.retrieve(
        existingPayment.stripe_session_id
      );

      if (!existingSession.url) {
         console.log("Idempotency key " + idempotency_key + " has expired.");
        return res.status(400).json({
          error: "Existing payment session has expired. Please try again later."
        });
      }

      return res.status(200).json({
        url: existingSession.url,
        reused: true
      });
    }
  } else {
    // Hvis ingen betaling med denne idempotencyKey, tjek for booking_id, som fallback
    const paymentByBooking = await Payment.findOne({
      where: { booking_id }
    });

    if (paymentByBooking?.status === "COMPLETED") {
      return res.status(409).json({
        error: "Payment has already been completed for this booking."
      });
    }

    if (paymentByBooking?.status === "PENDING" && paymentByBooking.stripe_session_id) {
      const existingSession = await stripe.checkout.sessions.retrieve(
        paymentByBooking.stripe_session_id
      );

      if (!existingSession.url) {
        return res.status(400).json({
          error: "Existing payment session has expired. Please create a new booking or try again later."
        });
      }

      return res.status(200).json({
        url: existingSession.url,
        reused: true
      });
    }
  }


} catch (error) {
  console.error("Payment handling error:", error);
  return res.status(500).json({ error: "Internal server error" });
}


    // payment does not already exist so create a new checkout session
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

        success_url:
          "http://localhost:3001/api/payment/stripe/success?session_id={CHECKOUT_SESSION_ID}",

        cancel_url:
          `http://localhost:3001/api/payment/stripe/cancel?booking_id=${booking_id}`,

        metadata: {
          booking_id: String(booking_id),
          user_id: String(user_id),
          userEmail: userEmail || ""
        }
      },
      {
        idempotency_key 
      }
    );

    // log new payment in the database after the checkout flow is completed
      await Payment.create({
        booking_id,
        user_id,
        idempotency_key: idempotency_key,
        amount,
        currency,
        status: session.payment_status,
        stripe_session_id: session.id
      });
    

    return res.status(200).json({
      url: session.url,
      reused: false
    });
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

    const existingPayment = await Payment.findOne({
      where: { booking_id }
    });

    if (!existingPayment) {
      return res.status(404).json({
        error: "Payment not found"
      });
    }

    if (existingPayment.status === "COMPLETED") {
      return res.status(200).json({
        booking_id,
        isPaid: true,
        status: "COMPLETED",
        reused: true
      });
    }

    await existingPayment.update({
      status: "COMPLETED"
    });

    await sendNotification({
      fromName: "Airport Payment Service",
      toEmail: email,
      subject: "Payment successful",
      body: `Payment for booking ${booking_id} was successful.`
    });

    return res.status(200).json({
      booking_id,
      isPaid: true,
      status: "COMPLETED"
    });
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

  try {
    const existingPayment = await Payment.findOne({
      where: { booking_id }
    });

    if (!existingPayment) {
      return res.status(404).json({
        error: "Payment not found"
      });
    }

    if (existingPayment.status === "COMPLETED") {
      return res.status(409).json({
        error: "Payment has already been completed for this booking."
      });
    }

    await existingPayment.update({
      status: "PENDING"
    });

    return res.status(200).json({
      booking_id,
      isPaid: false,
      status: "PENDING"
    });
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
      ]
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
