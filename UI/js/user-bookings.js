const BOOKING_STATUS = { 0: "pending", 1: "confirmed", 2: "cancelled" };

function formatDate(dateStr) {
    const date = new Date(dateStr);

    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");

    return `${hours}:${minutes}, ${day}.${month}.${year}`;
}

function createBookingCard(booking) {
    const statusLabel = BOOKING_STATUS[booking.status] ?? "unknown";
    const statusClass = statusLabel === "confirmed" ? "status-confirmed" : "status-pending";

    return `
        <div class="user-booking-card">

            <div class="booking-logo-box">
                <img src="../img/${booking.logo}" alt="${booking.arrivalAirport}">
            </div>

            <div class="booking-info-box">
                <span class="booking-status ${statusClass}">
                    ${statusLabel}
                </span>

                <h3>${booking.from} → ${booking.to}</h3>

                <p class="tm-text-highlight">
                    ${formatDate(booking.departureTime)} - ${formatDate(booking.arrivalTime)}
                </p>

                <p class="tm-text-gray">
                    ${booking.departureAirport} to ${booking.arrivalAirport}
                </p>

                <p>
                    <strong>Booking ID:</strong> #${booking.bookingId}
                </p>
            </div>

            <div class="booking-action-box">
                <p class="booking-price">${booking.price} DKK</p>

                <a href="flight-details.html?id=${booking.flightId}" class="booking-btn">
                    View Flight
                </a>
            </div>

        </div>
    `;
}

async function fetchUserBookings(userId, token) {
    const response = await fetch(`/api/Booking/user/${userId}`, {
        headers: { "Authorization": `Bearer ${token}` }
    });
    if (!response.ok) throw new Error(`Bookings request failed: ${response.status}`);
    return response.json();
}

async function fetchFlightsById(flightIds, token) {
    const idsFilter = flightIds.map(id => `"${id}"`).join(", ");
    const response = await fetch("/graphql", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({
            query: `
                query {
                    flights {
                        id
                        origin
                        destination
                        scheduledTime
                    }
                }
            `
        })
    });
    const result = await response.json();
    const flights = result.data?.flights || [];
    return Object.fromEntries(flights.map(f => [f.id, f]));
}

async function renderBookings() {
    const container = document.getElementById("bookingsContainer");
    const token = localStorage.getItem("access_token");
    const user = JSON.parse(localStorage.getItem("user_info") || "{}");

    if (!token || !user.sub) {
        container.innerHTML = `
            <div class="flight-info-box">
                <h3>Please log in to view your bookings</h3>
                <a href="login.html" class="btn btn-primary mt-3">Log in</a>
            </div>`;
        return;
    }

    let bookings;
    try {
        bookings = await fetchUserBookings(user.sub, token);
    } catch (err) {
        container.innerHTML = `<div class="flight-info-box"><h3>Could not load bookings</h3><p>${err.message}</p></div>`;
        return;
    }

    if (!bookings.length) {
        container.innerHTML = `
            <div class="flight-info-box">
                <h3>No bookings found</h3>
                <p>You have not booked any flights yet.</p>
                <a href="../index.html" class="btn btn-primary mt-3">Find flights</a>
            </div>`;
        return;
    }

    const flightIds = [...new Set(bookings.map(b => b.flightId))];
    const flightsMap = await fetchFlightsById(flightIds, token);

    container.innerHTML = bookings.map(booking => {
        const flight = flightsMap[booking.flightId] || {};
        return createBookingCard({
            bookingId: booking.bookingId,
            flightId: booking.flightId,
            from: flight.origin || "Unknown",
            to: flight.destination || "Unknown",
            departureAirport: flight.origin || "-",
            arrivalAirport: flight.destination || "-",
            departureTime: flight.scheduledTime || booking.createdAt,
            arrivalTime: flight.scheduledTime || booking.createdAt,
            logo: "cph.png",
            price: booking.totalPrice,
            status: booking.status
        });
    }).join("");
}

document.querySelector(".tm-current-year").textContent = new Date().getFullYear();

document.addEventListener("DOMContentLoaded", renderBookings);
