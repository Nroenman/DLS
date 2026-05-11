const express = require("express");
const router = express.Router();

const paymentController = require("../controllers/paymentController");
router.post("/stripe/webhook", express.raw({ type: "application/json" }), paymentController.stripe.webhook);


module.exports = router;