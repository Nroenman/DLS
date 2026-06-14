// const bookings = [
//     {
//         bookingId: 1001,
//         flightId: 1,
//         from: "Copenhagen",
//         to: "Bangkok",
//         departureAirport: "CPH",
//         arrivalAirport: "BKK",
//         departureTime: "2018-04-18 13:00:00",
//         arrivalTime: "2018-04-19 15:00:00",
//         logo: "bkk.png",
//         price: 499,
//         status: "confirmed"
//     },
//     {
//         bookingId: 1002,
//         flightId: 3,
//         from: "Copenhagen",
//         to: "Paris",
//         departureAirport: "CPH",
//         arrivalAirport: "CDG",
//         departureTime: "2018-04-20 07:15:00",
//         arrivalTime: "2018-04-20 09:30:00",
//         logo: "cdg.jpg",
//         price: 249,
//         status: "pending"
//     },
//     {
//         bookingId: 1003,
//         flightId: 8,
//         from: "Copenhagen",
//         to: "Tokyo",
//         departureAirport: "CPH",
//         arrivalAirport: "HND",
//         departureTime: "2018-04-25 10:00:00",
//         arrivalTime: "2018-04-26 08:00:00",
//         logo: "hnd.svg",
//         price: 699,
//         status: "confirmed"
//     }
// ];

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
    const statusClass = booking.status === "confirmed"
        ? "status-confirmed"
        : "status-pending";

    return `
        <div class="user-booking-card">

            <div class="booking-logo-box">
                <img src="../img/${booking.logo}" alt="${booking.arrivalAirport}">
            </div>

            <div class="booking-info-box">
                <span class="booking-status ${statusClass}">
                    ${booking.status}
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
                <p class="booking-price">$${booking.price}</p>

                <a href="flight-details.html?id=${booking.flightId}" class="booking-btn">
                    View Flight
                </a>
            </div>

        </div>
    `;
}

// function renderBookings() {
//     const container = document.getElementById("bookingsContainer");

//     if (!bookings.length) {
//         container.innerHTML = 
//             <div class="flight-info-box">
//                 <h3>No bookings found</h3>
//                 <p>You have not booked any flights yet.</p>
//                 <a href="../index.html" class="btn btn-primary mt-3">Find flights</a>
//             </div>
//         ;
//         return;
//     }

//     container.innerHTML = bookings.map(createBookingCard).join("");
// }

document.querySelector(".tm-current-year").textContent = new Date().getFullYear();

renderBookings();