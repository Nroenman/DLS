import asyncio
import os

import httpx
from contextlib import asynccontextmanager
from fastapi import FastAPI
from pydantic import BaseModel

OLLAMA_URL = os.getenv("OLLAMA_URL", "http://ollama:11434")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "qwen2.5:3b")
FLIGHT_GRAPHQL_URL = os.getenv("FLIGHT_GRAPHQL_URL", "http://flight:8080/graphql")

FLIGHTS_QUERY = """
{
  flights {
    flightNumber
    airline
    origin
    destination
    direction
    status
    scheduledDeparture
    scheduledArrival
    actualDeparture
    actualArrival
    delayReason
    gate {
      gateNumber
      terminal
    }
  }
}
"""

SYSTEM_PROMPT = """You are a flight information assistant for a private airport management system.

Rules you must follow without exception:
1. Your ONLY source of truth is the live flight data listed below. Ignore everything your training knows about real-world airports, airlines, or routes.
2. Every flight in the list is real and valid for this system — do not question, correct, or second-guess any flight number, airline name, origin, or destination.
3. If a passenger asks about a flight that appears in the data, answer using only that data.
4. If a flight is not in the data, say it is not found in the current schedule. Do not invent details.
5. Never mention Heathrow, or any airport not present in the data below, as a hub or assumption.
6. Be concise and friendly.

Live flight schedule:
{flight_data}"""

model_ready = False


async def _pull_model() -> None:
    global model_ready
    print(f"Waiting for Ollama at {OLLAMA_URL}...")
    async with httpx.AsyncClient(timeout=600) as client:
        for _ in range(60):
            try:
                await client.get(f"{OLLAMA_URL}/api/tags")
                break
            except Exception:
                await asyncio.sleep(5)

        print(f"Pulling {OLLAMA_MODEL} — this may take a few minutes on first run...")
        resp = await client.post(
            f"{OLLAMA_URL}/api/pull",
            json={"name": OLLAMA_MODEL, "stream": False},
        )
        resp.raise_for_status()

    print(f"Model {OLLAMA_MODEL} is ready.")
    model_ready = True


@asynccontextmanager
async def lifespan(app: FastAPI):
    asyncio.create_task(_pull_model())
    yield


app = FastAPI(lifespan=lifespan)


class ChatRequest(BaseModel):
    message: str


class ChatResponse(BaseModel):
    reply: str


async def _fetch_flights() -> list[dict]:
    async with httpx.AsyncClient(timeout=10) as client:
        resp = await client.post(FLIGHT_GRAPHQL_URL, json={"query": FLIGHTS_QUERY})
        resp.raise_for_status()
        data = resp.json()
    return data.get("data", {}).get("flights", [])


def _filter_flights(message: str, flights: list[dict]) -> list[dict]:
    """Return only the flights mentioned in the message, or all if the query is general."""
    msg = message.lower()

    # Exact flight-number match (e.g. "SK123")
    by_number = [f for f in flights if f["flightNumber"].lower() in msg]
    if by_number:
        return by_number

    # Match by origin or destination city/airport token
    by_place = [
        f for f in flights
        if any(token in msg for token in _tokens(f["origin"]))
        or any(token in msg for token in _tokens(f["destination"]))
    ]
    if by_place:
        return by_place

    return flights  # general query — give everything


def _tokens(place: str) -> list[str]:
    """Lower-case words and IATA codes from a place string."""
    import re
    parts = re.split(r"[\s,()]+", place.lower())
    return [p for p in parts if p]


def _format_flights(flights: list[dict]) -> str:
    if not flights:
        return "No flights currently in the system."
    lines = []
    for f in flights:
        gate  = f.get("gate")
        gate_str  = f"Terminal {gate['terminal']} Gate {gate['gateNumber']}" if gate else "TBD"
        delay_str = f"\n  Delay reason: {f['delayReason']}" if f.get("delayReason") else ""
        actual    = f.get("actualDeparture") or f.get("actualArrival")
        actual_str = f"\n  Actual time: {actual}" if actual else ""
        lines.append(
            f"- Flight {f['flightNumber']} operated by {f['airline']}\n"
            f"  From: {f['origin']}\n"
            f"  To:   {f['destination']}\n"
            f"  Direction: {f['direction']}\n"
            f"  Scheduled: {f['scheduledDeparture']}\n"
            f"  Status: {f['status']}\n"
            f"  Gate: {gate_str}"
            f"{actual_str}{delay_str}"
        )
    return "\n\n".join(lines)


@app.get("/assistant/health")
async def health():
    return {"status": "ok", "model_ready": model_ready}


@app.post("/assistant/chat", response_model=ChatResponse)
async def chat(req: ChatRequest):
    if not model_ready:
        return ChatResponse(reply="I'm still loading — please try again in a moment.")

    try:
        all_flights = await _fetch_flights()
        relevant    = _filter_flights(req.message, all_flights)
        flight_data = _format_flights(relevant)
    except Exception:
        flight_data = "Flight data is temporarily unavailable."

    system = SYSTEM_PROMPT.format(flight_data=flight_data)

    async with httpx.AsyncClient(timeout=60) as client:
        resp = await client.post(
            f"{OLLAMA_URL}/api/chat",
            json={
                "model": OLLAMA_MODEL,
                "messages": [
                    {"role": "system", "content": system},
                    {"role": "user", "content": req.message},
                ],
                "stream": False,
            },
        )
        resp.raise_for_status()

    return ChatResponse(reply=resp.json()["message"]["content"])
