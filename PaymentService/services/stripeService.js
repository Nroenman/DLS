require('dotenv').config();
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

const webhook = async (req, res) => 
{

  let paymentSession = {};

  // Webhook
  const sig = req.headers["stripe-signature"];

  let event;
  try {
    event = stripe.webhooks.constructEvent(req.body, sig, process.env.STRIPE_WEBHOOK_SECRET);
  } catch (err) {
    return res.status(400).send(`Webhook error: ${err.message}`);
  }

  console.log('EVENT TYPE WAS: '+event.type);

  switch (event.type) {
    case "checkout.session.completed": {
     
      res.send('Success page');
      break;
    }
    case "payment_intent.payment_failed":
      req.send('Failure page');

    case "checkout.session.expired":
      // NOT COMPLETED (abandoned)
      break;
  }

  res.json(paymentSession);
}

const stripeCheckout = async (req, res) => 
{
      
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
      success_url: `http://localhost:3000/api/payment/stripe/success`,
      cancel_url: `http://localhost:3000/api/payment/stripe/cancel`
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

const successRedirect = (req, res) => 
{
  //Send payment status to booking service here

  // Send notification of payment status to notification service

  // Temperary response 
  res.send("success");

}
  const cancelRedirect = (req, res) => 
  {

    //Send payment status to booking service here

    // Send notification of payment status to notification service

    // Temperary response
    res.send("cancel");  
  }
    

module.exports = {stripePayment, stripeCheckout, successRedirect , cancelRedirect, webhook};