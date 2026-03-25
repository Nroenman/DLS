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
            await stripeService.success(req, res);
        },
        cancel: async (req, res) => {
            await stripeService.cancel(req, res);
        }
    },
    paypal: {
        checkout: null,
        payment: null
    } // upcoming feature
};