const { DataTypes } = require("sequelize");
const sequelize = require("../database/mysql");

const Payment = sequelize.define(
  "Payment",
  {
    id: {
      type: DataTypes.INTEGER,
      autoIncrement: true,
      primaryKey: true
    },

    booking_id: {
      type: DataTypes.STRING(255),
      allowNull: false
    },

    user_id: {
      type: DataTypes.STRING(255),
      allowNull: false
    },

    idempotency_key: {
      type: DataTypes.STRING(255),
      allowNull: false,
      unique: true
    },

    amount: {
      type: DataTypes.INTEGER,
      allowNull: false
    },

    currency: {
      type: DataTypes.STRING(10),
      allowNull: false,
      defaultValue: "DKK"
    },

    status: {
      type: DataTypes.ENUM(
        "PENDING",
        "COMPLETED",
        "FAILED"
      ),
      allowNull: false,
      defaultValue: "PENDING"
    },

    stripe_session_id: {
      type: DataTypes.STRING(255),
      unique: true
    }
  },
  {
    tableName: "payments",
    timestamps: true,
    createdAt: "created_at",
    updatedAt: "updated_at"
  }
);

module.exports = Payment;