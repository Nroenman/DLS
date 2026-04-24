const express = require("express");
const router = express.Router();
const paymentController = require("../controllers/paymentController");

router.post('/stripe/checkout', paymentController.stripe.checkout);

router.post("/stripe/payment", paymentController.stripe.payment);

router.get('/stripe/success/:flightID', paymentController.stripe.success);
router.get('/stripe/cancel/:flightID', paymentController.stripe.cancel);

module.exports = router;