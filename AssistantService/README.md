# Assistant Service

Python/FastAPI service that answers passenger questions about flights in natural language. On each request it fetches the live flight list from the flight service, filters it to only flights relevant to the question, injects the result into a system prompt, and queries a locally-running Ollama instance. No external API calls — fully offline.

**Model**: `qwen2.5:3b` (pulled automatically on first start, ~2 GB, cached in a Docker volume)

---

## Endpoints

### `GET /assistant/health`

Returns the service health and whether the LLM model is loaded and ready.

```json
{ "status": "ok", "model_ready": true }
```

The frontend polls this until `model_ready` is `true` before enabling the chat input. The model takes 1–2 minutes to load on first start.

### `POST /assistant/chat`

Send a question, receive an answer.

**Request**

```json
{ "message": "When does SK501 depart?" }
```

**Response**

```json
{ "response": "SK501 is a SAS departure from CPH to AMS scheduled at 14:30." }
```

The service fetches fresh flight data on every request so the answer always reflects the current schedule.

---

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OLLAMA_URL` | `http://ollama:11434` | Ollama API base URL |
| `OLLAMA_MODEL` | `qwen2.5:3b` | Model name to pull and use |
| `FLIGHT_GRAPHQL_URL` | `http://flight:8080/graphql` | Flight service GraphQL endpoint |
| `PYTHONUNBUFFERED` | — | Set to `1` in Docker so logs are streamed immediately |

---

## Running locally

```bash
cd AssistantService
pip install -r requirements.txt

# Ollama must be running separately
OLLAMA_URL=http://localhost:11434 \
FLIGHT_GRAPHQL_URL=http://localhost:5000/graphql \
uvicorn main:app --host 0.0.0.0 --port 8080
```

Start Ollama with:

```bash
ollama serve
ollama pull qwen2.5:3b
```

---

## How context filtering works

A 3B model has limited context and drifts to training priors (e.g. assuming well-known airports) when given too much unrelated data. The service filters the full flight list before injecting it:

1. Tokenise the user message into lowercase words
2. Keep only flights whose flight number, origin, or destination match any token
3. Fall back to the full list for general questions (no specific flight mentioned)

This keeps the injected context small and relevant, preventing the model from hallucinating details not in the data.
