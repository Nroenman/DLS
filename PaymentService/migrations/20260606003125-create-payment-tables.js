"use strict";

module.exports = {
  async up(queryInterface, Sequelize) {
    await queryInterface.createTable("payments", {
      id: {
        type: Sequelize.INTEGER,
        autoIncrement: true,
        primaryKey: true,
        allowNull: false
      },

      booking_id: {
        type: Sequelize.STRING(255),
        allowNull: false
      },

      user_id: {
        type: Sequelize.STRING(255),
        allowNull: false
      },

      idempotency_key: {
        type: Sequelize.STRING(255),
        allowNull: false,
        unique: true
      },

      amount: {
        type: Sequelize.INTEGER,
        allowNull: false
      },

      currency: {
        type: Sequelize.STRING(10),
        allowNull: false,
        defaultValue: "DKK"
      },

      status: {
        type: Sequelize.ENUM("PENDING", "COMPLETED", "FAILED"),
        allowNull: false,
        defaultValue: "PENDING"
      },

      stripe_session_id: {
        type: Sequelize.STRING(255),
        allowNull: true,
        unique: true
      },

      created_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal("CURRENT_TIMESTAMP")
      },

      updated_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal(
          "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP"
        )
      }
    });

    await queryInterface.createTable("stripe_events", {
      event_id: {
        type: Sequelize.STRING(255),
        primaryKey: true,
        allowNull: false
      },

      event_type: {
        type: Sequelize.STRING(255),
        allowNull: true
      },

      processed_at: {
        type: Sequelize.DATE,
        allowNull: false,
        defaultValue: Sequelize.literal("CURRENT_TIMESTAMP")
      }
    });
  },

  async down(queryInterface) {
    await queryInterface.dropTable("stripe_events");
    await queryInterface.dropTable("payments");
  }
};