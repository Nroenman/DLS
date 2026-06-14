const Payment = require("../models/Payment");
const { sendToQueue } = require("../rabbitmq/producer");

jest.mock("../models/Payment");
jest.mock("../rabbitmq/producer");

const mockCreateSession = jest.fn();
const mockRetrieveSession = jest.fn();

jest.mock("stripe", () => {
  return jest.fn().mockImplementation(() => ({
    checkout: {
      sessions: {
        create: mockCreateSession,
        retrieve: mockRetrieveSession
      }
    }
  }));
});

const {
  stripeCheckout,
  successRedirect
} = require("../services/stripeService");

const mockRes = () => {
  const res = {};
  res.status = jest.fn().mockReturnValue(res);
  res.json = jest.fn().mockReturnValue(res);
  return res;
};

beforeEach(() => {
  jest.clearAllMocks();
});

describe("stripeService", () => {
  test("stripeCheckout returns 400 when required fields are missing", async () => {
    const req = {
      body: {},
      headers: {}
    };

    const res = mockRes();

    await stripeCheckout(req, res);

    expect(res.status).toHaveBeenCalledWith(400);
    expect(res.json).toHaveBeenCalledWith({
      error: "BookingId, UserId, TotalPrice are required."
    });
  });

  test("stripeCheckout returns 409 if payment already completed with same idempotency key", async () => {
    Payment.findOne.mockResolvedValueOnce({
      status: "COMPLETED"
    });

    const req = {
      body: {
        BookingId: "booking-1",
        UserId: "user-1",
        TotalPrice: 500,
        ContactEmail: "test@test.com"
      },
      headers: {
        "idempotency-key": "checkout-booking-1"
      }
    };

    const res = mockRes();

    await stripeCheckout(req, res);

    expect(Payment.findOne).toHaveBeenCalledWith({
      where: { idempotency_key: "checkout-booking-1" }
    });

    expect(res.status).toHaveBeenCalledWith(409);
    expect(res.json).toHaveBeenCalledWith({
      error: "Payment has already been completed for this request."
    });
  });

  test("stripeCheckout reuses existing pending Stripe session", async () => {
    Payment.findOne.mockResolvedValueOnce({
      status: "PENDING",
      stripe_session_id: "cs_test_123"
    });

    mockRetrieveSession.mockResolvedValueOnce({
      url: "https://checkout.stripe.com/test-session"
    });

    const req = {
      body: {
        BookingId: "booking-1",
        UserId: "user-1",
        TotalPrice: 500,
        ContactEmail: "test@test.com"
      },
      headers: {
        "idempotency-key": "checkout-booking-1"
      }
    };

    const res = mockRes();

    await stripeCheckout(req, res);

    expect(mockRetrieveSession).toHaveBeenCalledWith("cs_test_123");

    expect(res.status).toHaveBeenCalledWith(200);
    expect(res.json).toHaveBeenCalledWith({
      url: "https://checkout.stripe.com/test-session",
      reused: true
    });
  });

  test("stripeCheckout creates new Stripe session and payment when no existing payment exists", async () => {
    Payment.findOne
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce(null);

    mockCreateSession.mockResolvedValueOnce({
      id: "cs_test_new",
      url: "https://checkout.stripe.com/new-session"
    });

    const req = {
      body: {
        BookingId: "booking-1",
        UserId: "user-1",
        TotalPrice: 500,
        ContactEmail: "test@test.com"
      },
      headers: {
        "idempotency-key": "checkout-booking-1"
      }
    };

    const res = mockRes();

    await stripeCheckout(req, res);

    expect(mockCreateSession).toHaveBeenCalledWith(
      expect.objectContaining({
        mode: "payment",
        client_reference_id: "booking-1",
        line_items: expect.any(Array),
        metadata: expect.objectContaining({
          booking_id: "booking-1",
          user_id: "user-1",
          userEmail: "test@test.com"
        })
      }),
      {
        idempotencyKey: "checkout-booking-1"
      }
    );

    expect(Payment.create).toHaveBeenCalledWith({
      booking_id: "booking-1",
      user_id: "user-1",
      idempotency_key: "checkout-booking-1",
      amount: 50000,
      currency: "DKK",
      status: "PENDING",
      stripe_session_id: "cs_test_new"
    });

    expect(res.status).toHaveBeenCalledWith(200);
    expect(res.json).toHaveBeenCalledWith({
      url: "https://checkout.stripe.com/new-session",
      reused: false
    });
  });

  test("successRedirect marks payment as completed and sends queue messages", async () => {
    const mockUpdate = jest.fn();

    mockRetrieveSession.mockResolvedValueOnce({
      metadata: {
        userEmail: "test@test.com",
        booking_id: "booking-1"
      }
    });

    Payment.findOne.mockResolvedValueOnce({
      status: "PENDING",
      update: mockUpdate
    });

    const req = {
      query: {
        session_id: "cs_test_123"
      }
    };

    const res = mockRes();

    await successRedirect(req, res);

    expect(mockRetrieveSession).toHaveBeenCalledWith("cs_test_123");

    expect(mockUpdate).toHaveBeenCalledWith({
      status: "COMPLETED"
    });

    expect(sendToQueue).toHaveBeenCalledWith("booking_queue", {
      BookingId: "booking-1",
      PaymentSucceeded: true
    });

    expect(sendToQueue).toHaveBeenCalledWith("Notification", {
      fromName: "Airport Payment Service",
      toEmail: "test@test.com",
      subject: "Payment successful",
      body: "Payment for booking booking-1 was successful."
    });

    expect(res.status).toHaveBeenCalledWith(200);
    expect(res.json).toHaveBeenCalledWith({
      booking_id: "booking-1",
      isPaid: true,
      status: "COMPLETED"
    });
  });
});