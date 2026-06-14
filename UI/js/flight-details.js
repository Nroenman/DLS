const API_URL = "http://localhost:5000/graphql";

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

function getFlightIdFromUrl() {
    const params = new URLSearchParams(window.location.search);
    return params.get("id");
}

function getAirportCode(value) {
    if (!value) return "";
    const match = value.match(/\((.*?)\)/);
    return match ? match[1] : value;
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
                        delayReason
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

    if (result.errors) {
        console.error("GraphQL errors:", result.errors);
    }

    const flights = result.data?.flights || [];

    return flights.find(f => f.id === id);
}

async function renderFlightDetails() {
    const flightId = getFlightIdFromUrl();
    const container = document.getElementById("flightDetails");

    if (!flightId) {
        container.innerHTML = `
            <h2>Missing flight ID</h2>
            <a href="../index.html" class="btn btn-primary mt-3">Back to flights</a>
        `;
        return;
    }

    let flight;

    try {
        flight = await fetchFlightById(flightId);
    } catch (error) {
        container.innerHTML = `
            <h2>Could not load flight</h2>
            <p>${error.message}</p>
            <a href="../index.html" class="btn btn-primary mt-3">Back to flights</a>
        `;
        return;
    }

    if (!flight) {
        container.innerHTML = `
            <h2>Flight not found</h2>
            <a href="../index.html" class="btn btn-primary mt-3">Back to flights</a>
        `;
        return;
    }

    const originCode = getAirportCode(flight.origin);
    const destinationCode = getAirportCode(flight.destination);

    const gateText = flight.gate
        ? `Gate ${flight.gate.gateNumber}, Terminal ${flight.gate.terminal}`
        : "Gate not assigned";

    container.innerHTML = `
        <div class="mb-3">
            <button onclick="goBack()" class="btn btn-primary">
                <i class="fa fa-arrow-left"></i> Back
            </button>
        </div>

        <div class="flight-header">
            <h1 class="flight-route-title">
                ${flight.origin} → ${flight.destination}
            </h1>
            <span class="flight-badge">${flight.status}</span>
        </div>

        <div class="row flight-info-box">
            <div class="col-md-3 text-center">
                <img src="../img/cph.png" class="flight-airport-logo">
                <h5 class="mt-3">${originCode}</h5>
            </div>

            <div class="col-md-6">
                <h3>${flight.flightNumber} - ${flight.airline}</h3>

                <div class="flight-meta">
                    <p><i class="fa fa-plane"></i> From: ${flight.origin}</p>
                    <p><i class="fa fa-map-marker"></i> To: ${flight.destination}</p>
                    <p><i class="fa fa-clock-o"></i> Departure: ${formatDate(flight.scheduledDeparture)}</p>
                    <p><i class="fa fa-clock-o"></i> Arrival: ${formatDate(flight.scheduledArrival)}</p>
                    <p><i class="fa fa-info-circle"></i> Direction: ${flight.direction}</p>
                    <p><i class="fa fa-map-signs"></i> ${gateText}</p>
                    ${flight.delayReason ? `<p><i class="fa fa-warning"></i> Delay reason: ${flight.delayReason}</p>` : ""}
                </div>
            </div>

            <div class="col-md-3 text-center">
                <img src="../img/plane.png" class="flight-airport-logo">
                <h5 class="mt-3">${destinationCode}</h5>
            </div>
        </div>

        <div class="booking-box">
            <h2>${flight.status}</h2>
            <p>Secure your seat now.</p>

<button class="btn-book-flight" onclick="createBooking('${flight.id}')">
    Book flight 
</button>

        </div>

        <div class="text-center mt-4">
            <a href="../index.html" class="btn btn-primary">
                Browse more flights
            </a>
        </div>
    `;
}

async function createBooking(flightId) {
    const token = localStorage.getItem("access_token");
    const user = JSON.parse(localStorage.getItem("user_info") || "{}");

    if (!token) {
        alert("You must be logged in to buy a ticket.");
        window.location.href = "login.html";
        return;
    }

    try {

        // ── Create booking ───────────────────────────────
        const bookingRequest = {
            flightId: flightId,
            returnFlightId: null,
            isOneWay: true,
            seatClass: 0,
            contactEmail: user.email || "test@example.com",
            contactPhone: "+4512345678",
            ticketPrice: 500,
            passengers: [
                {
                    firstName: user.given_name || "Test",
                    lastName: user.family_name || "User",
                    dateOfBirth: "1995-01-01T00:00:00Z",
                    passportNumber: "TEST123456",
                    nationality: "Denmark",
                    isLeadPassenger: true,
                    hasExtraBaggage: false
                }
            ]
        };

        const bookingResponse = await fetch("http://localhost:5000/api/Booking", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${token}`
            },
            body: JSON.stringify(bookingRequest)
        });

     const bookingText = await bookingResponse.text();

console.log("Booking status:", bookingResponse.status);
console.log("Booking response:", bookingText);

if (!bookingResponse.ok) {
    alert("Booking failed: " + bookingResponse.status + " - " + bookingText);
    return;
}
        const booking = JSON.parse(bookingText);

        window.location.href = `checkout.html?flightId=${flightId}&bookingId=${booking.bookingId}&email=${user.email}&phone=${user.phone_number}`;

    } catch (error) {
        console.error(error);
        alert("Unexpected error: " + error.message);
    }
}



function goBack() {
    if (document.referrer) {
        window.history.back();
    } else {
        window.location.href = "../index.html";
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const year = document.querySelector(".tm-current-year");
    if (year) year.textContent = new Date().getFullYear();

    renderFlightDetails();
});