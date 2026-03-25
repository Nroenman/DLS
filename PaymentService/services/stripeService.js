require('dotenv').config();
const Stripe = require("stripe");
const stripe = new Stripe(process.env.STRIPE_SECRET_KEY);

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

const success = (req, res) => res.send("success");
const cancel = (req, res) => res.send("cancel");  

module.exports = {stripePayment, stripeCheckout, success, cancel};