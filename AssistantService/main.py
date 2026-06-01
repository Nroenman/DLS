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

SYSTEM_PROMPT = """You are a helpful airport assistant. Answer passenger questions \
about flights, gates, and departure/arrival times using only the flight data below. \
Be concise and friendly. If a specific flight cannot be found in the data, say so clearly.

Current flight data:
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


async def _fetch_flights() -> str:
    async with httpx.AsyncClient(timeout=10) as client:
        resp = await client.post(FLIGHT_GRAPHQL_URL, json={"query": FLIGHTS_QUERY})
        resp.raise_for_status()
        data = resp.json()

    flights = data.get("data", {}).get("flights", [])
    if not flights:
        return "No flights currently in the system."

    lines = []
    for f in flights:
        gate = f.get("gate")
        gate_str = f"{gate['terminal']}-{gate['gateNumber']}" if gate else "TBD"
        delay_str = f" | Delay reason: {f['delayReason']}" if f.get("delayReason") else ""
        actual = f.get("actualDeparture") or f.get("actualArrival")
        actual_str = f" | Actual: {actual}" if actual else ""
        lines.append(
            f"{f['flightNumber']} ({f['airline']}) | {f['direction']} | "
            f"{f['origin']} → {f['destination']} | "
            f"Scheduled: {f['scheduledDeparture']} | "
            f"Status: {f['status']} | Gate: {gate_str}"
            f"{actual_str}{delay_str}"
        )

    return "\n".join(lines)


@app.get("/assistant/health")
async def health():
    return {"status": "ok", "model_ready": model_ready}


@app.post("/assistant/chat", response_model=ChatResponse)
async def chat(req: ChatRequest):
    if not model_ready:
        return ChatResponse(reply="I'm still loading — please try again in a moment.")

    try:
        flight_data = await _fetch_flights()
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
