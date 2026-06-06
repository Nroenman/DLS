/**
 * Stripe webhook service.
 *
 * Handles:
 * - checkout.session.completed
 * - checkout.session.expired
 *
 * Idempotence:
 * - Stripe may send the same event more than once.
 * - Every processed event.id is stored in stripe_events.
 * - If the event.id already exists, the event is ignored.
 */

require("dotenv").config();

const Stripe = require("stripe");
const stripe = Stripe(process.env.STRIPE_SECRET_KEY);

const Payment = require("../models/Payment");
const StripeEvent = require("../models/StripeEvent");

async function hasProcessedEvent(eventId) {
  const existingEvent = await StripeEvent.findByPk(eventId);
  return existingEvent !== null;
}

async function markEventAsProcessed(eventId, eventType) {
  await StripeEvent.create({
    event_id: eventId,
    event_type: eventType
  });
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

      await Payment.update(
        {
          status: "COMPLETED",
          stripe_session_id: session.id
        },
        {
          where: {
            booking_id: bookingId,
            user_id: userId,
            stripe_session_id: session.id
          }
        }
      );

      break;
    }

    case "checkout.session.expired": {
      const session = event.data.object;

      const bookingId = session.metadata.booking_id;
      const userId = session.metadata.user_id;

      await Payment.update(
        {
          status: "FAILED"
        },
        {
          where: {
            booking_id: bookingId,
            user_id: userId,
            stripe_session_id: session.id
          }
        }
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

    return res.json(result);
  } catch (err) {
    console.error("Error processing Stripe webhook:", err.message);

    return res.status(400).send(`Webhook Error: ${err.message}`);
  }
};

module.exports = {
  handleStripeWebhookRequest
};