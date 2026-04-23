const { checkout } = require("../routes/paymentRoutes");
const stripeService = require("../services/stripeService");

module.exports = {
    stripe: {
        checkout: async (req, res) => {
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
            await stripeService.webhook(req, res);
        }
    },
    paypal: {
        checkout: null,
        payment: null
    } // upcoming feature
};