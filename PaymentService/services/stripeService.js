require('dotenv').config();
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const sendNotification = async (status) => {
  // const response = await fetch("http://notification-service/api/notifications", {
  //   method: "POST",
  //   headers: {
  //     "Content-Type": "application/json"
  //   },
  //   body: JSON.stringify({
  //     type: "PAYMENT_SUCCESS",
  //     flightID
  //   })
  // });

  // mock response for now
  return status;
};

const stripeCheckout = async (req, res) => 
{

  const flightID = req.body.flightID;
      
  try {
    const session = await stripe.checkout.sessions.create({
      mode: "payment",
      line_items: [{
        price_data: {
          currency: "DKK",
          product_data: {
            name: "Testprodukt"
          },
          unit_amount: 5000
        },
        quantity: 1
      }],
      success_url: `http://localhost:3000/api/payment/stripe/success/${flightID}`,
      cancel_url: `http://localhost:3000/api/payment/stripe/cancel/${flightID}`
    });

    res.json({ url: session.url });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

const stripePayment = async (req, res) => 
{
      try {
    const paymentIntent = await stripe.paymentIntents.create({
      amount: 5000,
      currency: "DKK",
      automatic_payment_methods: {
        enabled: true
      }
    });

     res.json(paymentIntent).status(200);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
}

const successRedirect = async (req, res) => 
{
  const paymentStatus = {
    flightID: req.params.flightID,     
    isPaid: true};

  try {
    await sendNotification("PAYMENT_SUCCESS");
  } catch (error) {
    console.error("Failed to send notification: ", error);
    res.status(500).json({ error: error.message });
  }

  res.json(paymentStatus).status(200);
}
  const cancelRedirect = async (req, res) => 
  {
    try {
      await sendNotification("PAYMENT_CANCELLED");
    } catch (error) {
      console.error("Failed to send notification: ", error);
      res.status(500).json({ error: error.message });
    }

    res.json({
      flightID: req.params.flightID,
      isPaid: false
    }).status(200);
  }
    

module.exports = {stripePayment, stripeCheckout, successRedirect , cancelRedirect};