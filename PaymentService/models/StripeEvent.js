const { DataTypes } = require("sequelize");
const sequelize = require("../database/mysql");

const StripeEvent = sequelize.define(
  "StripeEvent",
  {
    event_id: {
      type: DataTypes.STRING(255),
      primaryKey: true
    },

    event_type: {
      type: DataTypes.STRING(255)
    },

    processed_at: {
      type: DataTypes.DATE,
      defaultValue: DataTypes.NOW
    }
  },
  {
    tableName: "stripe_events",
    timestamps: false
  }
);

module.exports = StripeEvent;