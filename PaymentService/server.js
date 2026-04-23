require('dotenv').config();
const express = require('express');
const app = express();
const bodyParser = require('body-parser');
const paymentRoutes = require('./routes/paymentRoutes');
const PORT = process.env.PORT || 3000;
const { webhook } = require('./services/stripeService');

app.use('/api/payment', paymentRoutes);
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(express.static("public"));

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});