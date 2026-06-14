const API_URL = "http://localhost:5000/graphql";

const TICKET_PRICE = 500;
const BAGGAGE_PRICE_PER_KG = 10;

function formatDate(dateStr) {
    if (!dateStr) return "Unknown";

    return new Date(dateStr).toLocaleString("en-GB", {
        hour: "2-digit",
        minute: "2-digit",
        day: "2-digit",
        month: "2-digit",
        year: "numeric"
    });
}

function getAllQueryParams() {
  const params = new URLSearchParams(window.location.search);
  const entries = params.entries(); // Iterator over [key, value]
  const result = {};
  for (const [key, value] of entries) {
    result[key] = value;
  }
  return result;
}

async function fetchFlightById(id) {
    const token = localStorage.getItem("access_token");

    const response = await fetch(API_URL, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...(token ? { "Authorization": `Bearer ${token}` } : {})
        },
        body: JSON.stringify({
            query: `
                query {
                    flights {
                        id
                        flightNumber
                        airline
                        origin
                        destination
                        status
                        direction
                        scheduledDeparture
                        scheduledArrival
                        gate {
                            gateNumber
                            terminal
                        }
                    }
                }
            `
        })
    });

    const result = await response.json();
    const flights = result.data?.flights || [];

    return flights.find(f => f.id === id);
}

function calculateTotal() {
    const baggageWeight = Number(document.getElementById("baggageWeight").value || 0);
    const baggagePrice = baggageWeight * BAGGAGE_PRICE_PER_KG;
    const total = TICKET_PRICE + baggagePrice;

    document.getElementById("baggagePrice").innerText = `${baggagePrice} DKK`;
    document.getElementById("totalPrice").innerText = `${total} DKK`;
}

async function renderCheckout() {
    const allQueryParams = getAllQueryParams();
    const flightId = allQueryParams.flightId;
    const container = document.getElementById("checkoutDetails");

    console.log(flightId);

    if (!flightId) {
        container.innerHTML = `<h2>Missing flight ID</h2>`;
        return;
    }

    const flight = await fetchFlightById(flightId);

    if (!flight) {
        container.innerHTML = `<h2>Flight not found</h2>`;
        return;
    }

    const gateText = flight.gate
        ? `Gate ${flight.gate.gateNumber}, Terminal ${flight.gate.terminal}`
        : "Gate not assigned";

    container.innerHTML = `
        <div class="mb-3">
            <a href="flight-details.html?id=${flight.id}" class="btn btn-primary">
                <i class="fa fa-arrow-left"></i> Back to flight
            </a>
        </div>

        <h1 class="checkout-title">Booking created! <br><br> Checkout</h1>

        <div class="checkout-box">
            <h3>${flight.flightNumber} - ${flight.airline}</h3>

            <div class="checkout-meta">
                <p><i class="fa fa-plane"></i> From: ${flight.origin}</p>
                <p><i class="fa fa-map-marker"></i> To: ${flight.destination}</p>
                <p><i class="fa fa-clock-o"></i> Departure: ${formatDate(flight.scheduledDeparture)}</p>
                <p><i class="fa fa-clock-o"></i> Arrival: ${formatDate(flight.scheduledArrival)}</p>
                <p><i class="fa fa-map-signs"></i> ${gateText}</p>
                <p><i class="fa fa-info-circle"></i> Status: ${flight.status}</p>
            </div>
        </div>

        <div class="checkout-box">
            <h3>Baggage</h3>

            <label for="baggageWeight">Baggage weight in kg</label>
<input
    type="number"
    id="baggageWeight"
    class="form-control"
    min="0"
    step="0.5"
    placeholder="Enter baggage weight"
    value="0"
    onclick="if(this.value==='0') this.value='';"
    oninput="calculateTotal()"
/>
        </div>

        <div class="payment-box">
            <h2>Payment</h2>

            <div class="price-line">
                <span>Ticket</span>
                <span>${TICKET_PRICE} DKK</span>
            </div>

            <div class="price-line">
                <span>Baggage</span>
                <span id="baggagePrice">0 DKK</span>
            </div>

            <div class="price-line price-total">
                <span>Total</span>
                <span id="totalPrice">${TICKET_PRICE} DKK</span>
            </div>

            <button class="btn-pay mt-4" onclick="payNow('${flight.id}')">
                Pay
            </button>
        </div>
    `;
}

async function payNow() {

    const allQueryParams = getAllQueryParams();
    console.log(allQueryParams);
    const bookingId = allQueryParams.bookingId;
    const email = allQueryParams.email;
    const phone = allQueryParams.phone;

    let idempotencyKey = localStorage.getItem(`idempotencyKey-${bookingId}`);
        
    if (!idempotencyKey) 
        {
            // Generer og gem ny idempotency key, hvis den ikke eksisterer
            idempotencyKey = `checkout-${bookingId}-${Date.now()}`;
            localStorage.setItem(`idempotencyKey-${bookingId}`, idempotencyKey);
        }

    const token = localStorage.getItem("access_token");
    const user = JSON.parse(localStorage.getItem("user_info") || "{}");
    if (!token) {
        alert("You must be logged in to pay.");
        window.location.href = "login.html";
        return;
    }
    const baggageWeight = Number(document.getElementById("baggageWeight").value || 0);
    const baggagePrice = baggageWeight * BAGGAGE_PRICE_PER_KG;
    const totalPrice = TICKET_PRICE + baggagePrice;
    try {
        // Check om idempotency key allerede er gemt for denne booking

        // Betalingskald med idempotencyKey
        const paymentResponse = await fetch(
            "http://localhost:5000/api/payment/stripe/checkout",
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Idempotency-Key": idempotencyKey
                },
                body: JSON.stringify({
                    BookingId: bookingId,
                    UserId: user.sub,
                    TotalPrice: totalPrice,
                    ContactEmail: email,
                    ContactPhone: phone
                })
            }
        );

        const paymentText = await paymentResponse.text();

        if (!paymentResponse.ok) {
            alert("Payment failed: " + paymentText);
            return;
        }

        const payment = JSON.parse(paymentText);

        if (!payment.url) {
            alert("Stripe checkout URL missing.");
            return;
        }

        window.location.href = payment.url;

    } catch (error) {
        console.error(error);
        alert("Unexpected error: " + error.message);
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const year = document.querySelector(".tm-current-year");
    if (year) year.textContent = new Date().getFullYear();

    renderCheckout();
});