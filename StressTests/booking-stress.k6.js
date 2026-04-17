import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
        stages: [
            { duration: "30s", target: 20 },
            { duration: "1m",  target: 100 },
            { duration: "2m",  target: 300 },
            { duration: "2m",  target: 600 },
            { duration: "2m",  target: 1000 },
            { duration: "1m",  target: 0 },
        ],
    thresholds: {
        http_req_failed:   ["rate<0.01"],
        http_req_duration: ["p(95)<500"],
    },
};

const payload = JSON.stringify({
    flightId: "Flight123",
    returnFlightId: null,
    isOneWay: true,
    seatClass: 0,
    contactEmail: "test@test.com",
    contactPhone: "12345678",
    ticketPrice: 1000,
    passengers: [
        {
            firstName: "Kader",
            lastName: "Kivrak",
            dateOfBirth: "1994-05-15",
            passportNumber: "Pass12345",
            nationality: "Danish",
            isLeadPassenger: true,
            hasExtraBaggage: false
        }
    ]
});

const headers = { "Content-Type": "application/json" };

export default function () {
    const res = http.post("http://localhost:5208/api/booking", payload, { headers });

    check(res, {
        "status 200": (r) => r.status === 200,
    });

    sleep(1);
}