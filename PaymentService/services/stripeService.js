require("dotenv").config();

const db = require("../database/mysql.js");
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const { sendToQueue } = require("../rabbitmq/producer");

const sendNotification = async (paymentStatus) => {
  await sendToQueue("payment_status_queue", paymentStatus);
};

const stripeCheckout = async (req, res) => {
  console.log("Received checkout request with body:", req.body);

  // Validate input start -- 
  const { booking_id, user_id, amount, currency } = req.body;

  if (!booking_id || !user_id || !amount || !currency) {
    return res.status(400).json({
      error: "All fields are required"
    });
  }
  // Validate input end -- 
  
  // For idempotency 
  const idempotencyKey = `checkout-${booking_id}`;

  // Check if payment already exists for this booking start -- 
  try {
    const [existingPayments] = await db.query(
      `SELECT *
       FROM payments
       WHERE booking_id = ?
       AND status = 'PENDING'
       LIMIT 1`,
      [booking_id]
    );

    if (existingPayments.length > 0) {
     
      const existingPayment = existingPayments[0];
      console.log("Existing pending payment found: ", existingPayment);
      
      if (existingPayment.stripe_session_id) {
        const existingSession = await stripe.checkout.sessions.retrieve(
          existingPayment.stripe_session_id
        );

        console.log("Retrieved existing session: ", existingSession);

        if(existingSession.url == null) {
          console.log("Existing session has expired.");
          res.status(400).json({
            error: "Existing payment session has expired. Please try again."
          });
          return;
        }

        return res.status(200).json({
          url: existingSession.url,
          reused: true
        });
      }
    }
    // Check if payment already exists for this booking end --

    // Create Stripe Checkout Session start --
    const session = await stripe.checkout.sessions.create(
      {
        mode: "payment",

        // Optional, but useful for Stripe dashboard/reconciliation
        client_reference_id: String(booking_id),

        line_items: [
          {
            price_data: {
              currency,
              product_data: {
                name: `Booking ${booking_id}`
              },
              unit_amount: amount
            },
            quantity: 1
          }
        ],

        success_url: `http://localhost:3001/api/payment/stripe/success/${booking_id}`,
        cancel_url: `http://localhost:3001/api/payment/stripe/cancel/${booking_id}`,

        metadata: {
          booking_id: String(booking_id),
          user_id: String(user_id)
        }
      },
      {
        idempotencyKey
      }
    );
    // Create Stripe Checkout Session end --

    // Save payment record with PENDING status start --
    await db.query(
      `INSERT INTO payments
       (booking_id, user_id, idempotency_key, amount, currency, status, stripe_session_id)
       VALUES (?, ?, ?, ?, ?, ?, ?)
       ON DUPLICATE KEY UPDATE
         stripe_session_id = VALUES(stripe_session_id),
         idempotency_key = VALUES(idempotency_key),
         status = 'PENDING',
         updated_at = CURRENT_TIMESTAMP`,
      [
        booking_id,
        user_id,
        idempotencyKey,
        amount,
        currency,
        "PENDING", // Will start as PENDING and update to SUCCESS if successRedirect is called
        session.id
      ]
    );
    // Save payment record with PENDING status end --

    // Return the session URL to the client
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
  const paymentStatus = {
    booking_id: req.params.booking_id,
    isPaid: true,
    status: "COMPLETED"
  };

  try {
    await db.query(
      `UPDATE payments
       SET status = ?,
           updated_at = CURRENT_TIMESTAMP
       WHERE booking_id = ?`,
      [paymentStatus.status, paymentStatus.booking_id]
    );
  } catch (error) {
    console.error("Failed to update payment status in database:", error.message);
    return res.status(500).json({ error: error.message });
  }

  try {
    await sendNotification(paymentStatus);
    res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Failed to send RabbitMQ notification:", error.message);
    res.status(500).json({ error: error.message });
  }
};

const cancelRedirect = async (req, res) => {
  const paymentStatus = {
    booking_id: req.params.booking_id,
    isPaid: false,
    status: "PENDING"
  };

  try {
  await db.query(
    `UPDATE payments
     SET status = ?,  
          updated_at = CURRENT_TIMESTAMP
      WHERE booking_id = ?`,
    [paymentStatus.status, paymentStatus.booking_id]
    );
  } catch (error) {
    console.error("Failed to update payment status in database:", error.message);
    return res.status(500).json({ error: error.message });
  }

  try {
    await sendNotification(paymentStatus);
    res.status(200).json(paymentStatus);
  } catch (error) {
    console.error("Failed to send RabbitMQ notification:", error.message);
    res.status(500).json({ error: error.message });
  }
};

module.exports = {
  stripeCheckout,
  successRedirect,
  cancelRedirect
};