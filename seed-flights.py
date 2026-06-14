#!/usr/bin/env python3
"""Seed the flight service with a realistic spread of departures and arrivals."""
import json, sys
from datetime import datetime, timezone, timedelta

try:
    import urllib.request as req
except ImportError:
    sys.exit("Python 3 required")

ENDPOINT = sys.argv[1] if len(sys.argv) > 1 else "http://localhost/graphql"

now = datetime.now(timezone.utc)
def t(delta): return (now + delta).strftime("%Y-%m-%dT%H:%M:%SZ")
def h(n): return timedelta(hours=n)
def m(n): return timedelta(minutes=n)

# (number, airline, origin, dest, time_delta, direction)
# For departures, time_delta is when the flight departs.
# For arrivals,   time_delta is when the flight lands.
FLIGHTS = [
    # ── Departures ─────────────────────────────────────────────────────────
    ("SK501",  "SAS",              "CPH", "AMS", m(30),    "DEPARTURE"),
    ("DY441",  "Norwegian",        "CPH", "OSL", h(1),     "DEPARTURE"),
    ("BA812",  "British Airways",  "CPH", "LHR", h(1)+m(30), "DEPARTURE"),
    ("KL1203", "KLM",              "CPH", "AMS", h(2),     "DEPARTURE"),
    ("LH2934", "Lufthansa",        "CPH", "FRA", h(2)+m(30), "DEPARTURE"),
    ("TK1876", "Turkish Airlines", "CPH", "IST", h(3),     "DEPARTURE"),
    ("SK947",  "SAS",              "CPH", "JFK", h(4),     "DEPARTURE"),
    ("AF1285", "Air France",       "CPH", "CDG", h(5),     "DEPARTURE"),
    ("EK153",  "Emirates",         "CPH", "DXB", h(6),     "DEPARTURE"),
    ("SK011",  "SAS",              "CPH", "BCN", h(8),     "DEPARTURE"),
    ("FR4421", "Ryanair",          "CPH", "STN", h(9),     "DEPARTURE"),
    ("U22843", "EasyJet",          "CPH", "GVA", h(11),    "DEPARTURE"),
    ("SK245",  "SAS",              "CPH", "NRT", h(24),    "DEPARTURE"),
    ("DL8734", "Delta",            "CPH", "JFK", h(26),    "DEPARTURE"),
    ("LH2938", "Lufthansa",        "CPH", "MUC", h(28),    "DEPARTURE"),
    ("SK503",  "SAS",              "CPH", "AMS", h(48),    "DEPARTURE"),

    # ── Arrivals ───────────────────────────────────────────────────────────
    ("SK502",  "SAS",              "AMS", "CPH", m(20),    "ARRIVAL"),
    ("DY442",  "Norwegian",        "OSL", "CPH", m(45),    "ARRIVAL"),
    ("KL1204", "KLM",              "AMS", "CPH", h(1),     "ARRIVAL"),
    ("BA813",  "British Airways",  "LHR", "CPH", h(1)+m(30), "ARRIVAL"),
    ("LH2935", "Lufthansa",        "FRA", "CPH", h(2),     "ARRIVAL"),
    ("TK1875", "Turkish Airlines", "IST", "CPH", h(3),     "ARRIVAL"),
    ("AF1284", "Air France",       "CDG", "CPH", h(4),     "ARRIVAL"),
    ("EK152",  "Emirates",         "DXB", "CPH", h(6),     "ARRIVAL"),
    ("FR4422", "Ryanair",          "STN", "CPH", h(8),     "ARRIVAL"),
    ("SK948",  "SAS",              "JFK", "CPH", h(10),    "ARRIVAL"),
    ("SK246",  "SAS",              "NRT", "CPH", h(22),    "ARRIVAL"),
    ("DL8733", "Delta",            "JFK", "CPH", h(25),    "ARRIVAL"),
]

MUTATION = """
mutation {{
  createFlight(input: {{
    flightNumber: "{num}"
    airline:      "{airline}"
    origin:       "{orig}"
    destination:  "{dest}"
    scheduledTime: "{time}"
    direction: {direction}
  }}) {{
    flight {{ id flightNumber status }}
  }}
}}
"""

ok = fail = 0
for num, airline, orig, dest, time_d, direction in FLIGHTS:
    time = t(time_d)
    label = f"{num:8s} {orig}→{dest} ({direction})"
    body = json.dumps({"query": MUTATION.format(
        num=num, airline=airline, orig=orig, dest=dest,
        time=time, direction=direction
    )}).encode()
    try:
        r = req.urlopen(req.Request(ENDPOINT, data=body,
                        headers={"Content-Type": "application/json"}))
        data = json.loads(r.read())
        if data.get("data", {}).get("createFlight"):
            print(f"  ✓ {label}")
            ok += 1
        else:
            print(f"  ✗ {label} — {data.get('errors')}")
            fail += 1
    except Exception as e:
        print(f"  ✗ {label} — {e}")
        fail += 1

print(f"\nDone — {ok} created, {fail} failed.")
