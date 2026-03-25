const router = require("express").Router();
const paymentController = require("../controllers/paymentController");

router.post('/stripe/checkout', paymentController.stripe.checkout);

router.post("/stripe/payment", paymentController.stripe.payment);

router.get('/stripe/success', paymentController.stripe.success);
router.get('/stripe/cancel', paymentController.stripe.cancel);

module.exports = router;