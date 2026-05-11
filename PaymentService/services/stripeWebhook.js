/**
 * Stripe webhook service.
 *
 * Stripe sends events to this endpoint when something happens in Checkout.
 * This service handles:
 * - checkout.session.completed
 * - checkout.session.expired
 *
 * Idempotence:
 * - Stripe may send the same event more than once.
 * - Therefore, every processed event.id is stored in stripe_events.
 * - If the event.id already exists, the event is ignored.
 */

require("dotenv").config();

const Stripe = require("stripe");
const stripe = Stripe(process.env.STRIPE_SECRET_KEY);

const db = require("./../database/mysql.js");

async function hasProcessedEvent(eventId) {
  const [rows] = await db.query(
    "SELECT event_id FROM stripe_events WHERE event_id = ?",
    [eventId]
  );

  return rows.length > 0;
}

async function markEventAsProcessed(eventId, eventType) {
  await db.query(
    "INSERT INTO stripe_events (event_id, event_type) VALUES (?, ?)",
    [eventId, eventType]
  );
}

async function handleStripeWebhook(rawBody, signature) {
  const event = stripe.webhooks.constructEvent(
    rawBody,
    signature,
    process.env.STRIPE_WEBHOOK_SECRET
  );

  console.log("Stripe event id:", event.id);
  console.log("Stripe event type:", event.type);

  if (await hasProcessedEvent(event.id)) {
    return {
      duplicate: true,
      eventId: event.id,
      type: event.type
    };
  }

  switch (event.type) {
    case "checkout.session.completed": {
      const session = event.data.object;

      const bookingId = session.metadata.booking_id;
      const userId = session.metadata.user_id;

      await db.query(
        `UPDATE payments
         SET status = ?,
             stripe_session_id = ?,
             updated_at = CURRENT_TIMESTAMP
         WHERE booking_id = ?
           AND user_id = ?
           AND stripe_session_id = ?`,
        [
          "COMPLETED",
          session.id,
          bookingId,
          userId,
          session.id
        ]
      );

      break;
    }

    case "checkout.session.expired": {
      const session = event.data.object;

      const bookingId = session.metadata.booking_id;
      const userId = session.metadata.user_id;

      await db.query(
        `UPDATE payments
         SET status = ?,
             stripe_session_id = ?,
             updated_at = CURRENT_TIMESTAMP
         WHERE booking_id = ?
           AND user_id = ?
           AND stripe_session_id = ?`,
        [
          "CANCELLED",
          session.id,
          bookingId,
          userId,
          session.id
        ]
      );

      break;
    }

    default:
      console.log(`Unhandled event type: ${event.type}`);
  }

  await markEventAsProcessed(event.id, event.type);

  return {
    duplicate: false,
    eventId: event.id,
    type: event.type
  };
}

const handleStripeWebhookRequest = async (req, res) => {
  try {
    console.log("Stripe Webhook Service called");

    const result = await handleStripeWebhook(
      req.body,
      req.headers["stripe-signature"]
    );

    console.log("Stripe Webhook processed event:", result);

    res.json(result);
  } catch (err) {
    console.error("Error processing Stripe webhook:", err.message);

    res.status(400).send(`Webhook Error: ${err.message}`);
  }
};

module.exports = {
  handleStripeWebhookRequest
};