const stripeService = require("../services/stripeService");
const stripeWebhookService = require("../services/stripeWebhook");

module.exports = {
  stripe: {
    checkout: async (req, res) => {
      console.log("Received checkout request with body:", req.body);
      await stripeService.stripeCheckout(req, res);
    },

    payment: async (req, res) => {
      await stripeService.stripePayment(req, res);
    },

    success: async (req, res) => {
      await stripeService.successRedirect(req, res);
    },

    cancel: async (req, res) => {
      await stripeService.cancelRedirect(req, res);
    },
    webhook: async (req, res) => {
      await stripeWebhookService.handleStripeWebhookRequest(req, res);
    }
  },

  paypal: {
    checkout: null,
    payment: null
  }
};