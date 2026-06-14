const API_URL = "http://localhost:5000/graphql";

function formatDate(dateStr) {
    if (!dateStr) return "Unknown time";

    const date = new Date(dateStr);
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");

    return `${hours}:${minutes}, ${day}.${month}.${year}`;
}

function getAirportCode(value) {
    if (!value) return "";
    const match = value.match(/\((.*?)\)/);
    return match ? match[1] : value;
}

function getContinentFromDestination(destination) {
    if (!destination) return "europe";

    const text = destination.toLowerCase();

    if (text.includes("dubai")) return "asia";
    if (text.includes("amsterdam")) return "europe";
    if (text.includes("frankfurt")) return "europe";
    if (text.includes("barcelona")) return "europe";
    if (text.includes("london")) return "europe";
    if (text.includes("paris")) return "europe";
    if (text.includes("madrid")) return "europe";

    return "europe";
}

function createFlightCard(flight) {
    const originCode = getAirportCode(flight.origin);
    const destinationCode = getAirportCode(flight.destination);

    const gateText = flight.gate
        ? `Gate ${flight.gate.gateNumber}, Terminal ${flight.gate.terminal}`
        : "Gate not assigned";

    const delayText = flight.delayReason
        ? `<p class="tm-text-highlight">Delay reason: ${flight.delayReason}</p>`
        : "";

    return `
        <div class="tm-recommended-place-wrap">
            <div class="tm-recommended-place">
                <img src="img/cph.png"
                     alt="${flight.flightNumber}"
                     class="img-fluid img-thumbnail tm-logo-medium tm-recommended-img">

                <div class="tm-recommended-description-box">
                    <h3 class="tm-recommended-title">
                        ${flight.flightNumber} - ${flight.airline}
                    </h3>

                    <p class="tm-text-highlight">
                        ${formatDate(flight.scheduledDeparture)} - ${formatDate(flight.scheduledArrival)}
                    </p>

                    <p class="tm-text-gray">
                        ${flight.origin} → ${flight.destination}
                    </p>

                    <p class="tm-text-gray">
                        Status: ${flight.status} | Direction: ${flight.direction}
                    </p>

                    <p class="tm-text-gray">
                        ${originCode} → ${destinationCode} | ${gateText}
                    </p>

                    ${delayText}
                </div>

                <a href="pages/flight-details.html?id=${flight.id}" class="tm-recommended-price-box">
                    <p class="tm-recommended-price">${flight.status}</p>
                    <p class="tm-recommended-price-link">View Flight</p>
                </a>
            </div>
        </div>
        <br>
    `;
}

async function fetchFlights() {
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

    return result.data?.flights || [];
}

function clearFlightTabs() {
    document.querySelectorAll("#asia, #europe, #north-america, #south-america, #africa, #australia, #antarctica")
        .forEach(tab => tab.innerHTML = "");
}

async function renderFlights() {
    clearFlightTabs();

    const flights = await fetchFlights();

    flights.forEach(flight => {
        const continent = getContinentFromDestination(flight.destination);
        const continentPane = document.getElementById(continent);

        if (continentPane) {
            continentPane.innerHTML += createFlightCard(flight);
        }
    });
}

document.addEventListener("DOMContentLoaded", renderFlights);