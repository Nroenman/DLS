const express = require("express");
const router = express.Router();

const paymentController = require("../controllers/paymentController");

router.post("/stripe/checkout", paymentController.stripe.checkout);

router.get("/stripe/success", paymentController.stripe.success);
router.get("/stripe/cancel", paymentController.stripe.cancel);

router.get("/user/:userId", paymentController.stripe.getPaymentsByUserId);
router.get("/booking/:bookingId", paymentController.stripe.getPaymentByBookingId);


module.exports = router;