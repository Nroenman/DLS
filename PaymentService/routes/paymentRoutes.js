const express = require("express");
const router = express.Router();

const paymentController = require("../controllers/paymentController");

router.post("/stripe/checkout", paymentController.stripe.checkout);
router.post("/stripe/payment", paymentController.stripe.payment);

router.get("/stripe/success/:booking_id", paymentController.stripe.success);
router.get("/stripe/cancel/:booking_id", paymentController.stripe.cancel);


router.post("/stripe/webhook", express.raw({ type: "application/json" }), paymentController.stripe.webhook);


module.exports = router;