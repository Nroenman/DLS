const stripeService = require("../services/stripeService");

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
    }
  },

  paypal: {
    checkout: null,
    payment: null
  }
};