#!/bin/bash
# Creates a spread of departure and arrival flights relative to now.
# Usage: bash seed-flights.sh [endpoint]
set -e

ENDPOINT="${1:-http://localhost/graphql}"

t() { date -u -d "$1" '+%Y-%m-%dT%H:%M:%SZ'; }

flight() {
  local num="$1" airline="$2" orig="$3" dest="$4" time="$5" dir="$6"
  printf '{"query":"mutation{createFlight(input:{flightNumber:\\"%s\\",airline:\\"%s\\",origin:\\"%s\\",destination:\\"%s\\",scheduledTime:\\"%s\\",direction:%s}){flight{id flightNumber status}}}"}' \
    "$num" "$airline" "$orig" "$dest" "$time" "$dir"
}

ok=0; fail=0
create() {
  local label="$1"; shift
  local body
  body=$(flight "$@")
  local resp
  resp=$(curl -sf -X POST "$ENDPOINT" -H "Content-Type: application/json" -d "$body")
  if echo "$resp" | grep -q '"flightNumber"'; then
    echo "  ✓ $label"
    (( ok++ )) || true
  else
    echo "  ✗ $label — $resp"
    (( fail++ )) || true
  fi
}

echo ""
echo "=== DEPARTUREs ==="

#          label              num      airline             orig  dest   scheduled time           dir
create "SK501  CPH→AMS  +30m"  SK501  "SAS"               CPH   AMS   "$(t '+30 minutes')"      DEPARTURE
create "DY441  CPH→OSL  +1h"   DY441  "Norwegian"         CPH   OSL   "$(t '+1 hour')"          DEPARTURE
create "BA812  CPH→LHR  +1h30" BA812  "British Airways"   CPH   LHR   "$(t '+1 hour 30 minutes')" DEPARTURE
create "KL1203 CPH→AMS  +2h"   KL1203 "KLM"               CPH   AMS   "$(t '+2 hours')"         DEPARTURE
create "LH2934 CPH→FRA  +2h30" LH2934 "Lufthansa"         CPH   FRA   "$(t '+2 hours 30 minutes')" DEPARTURE
create "TK1876 CPH→IST  +3h"   TK1876 "Turkish Airlines"  CPH   IST   "$(t '+3 hours')"         DEPARTURE
create "SK947  CPH→JFK  +4h"   SK947  "SAS"               CPH   JFK   "$(t '+4 hours')"         DEPARTURE
create "AF1285 CPH→CDG  +5h"   AF1285 "Air France"        CPH   CDG   "$(t '+5 hours')"         DEPARTURE
create "EK153  CPH→DXB  +6h"   EK153  "Emirates"          CPH   DXB   "$(t '+6 hours')"         DEPARTURE
create "SK011  CPH→BCN  +8h"   SK011  "SAS"               CPH   BCN   "$(t '+8 hours')"         DEPARTURE
create "FR4421 CPH→STN  +9h"   FR4421 "Ryanair"           CPH   STN   "$(t '+9 hours')"         DEPARTURE
create "U22843 CPH→GVA  +11h"  U22843 "EasyJet"           CPH   GVA   "$(t '+11 hours')"        DEPARTURE
create "SK245  CPH→NRT  +24h"  SK245  "SAS"               CPH   NRT   "$(t '+24 hours')"        DEPARTURE
create "DL8734 CPH→JFK  +26h"  DL8734 "Delta"             CPH   JFK   "$(t '+26 hours')"        DEPARTURE
create "LH2938 CPH→MUC  +28h"  LH2938 "Lufthansa"         CPH   MUC   "$(t '+28 hours')"        DEPARTURE
create "SK503  CPH→AMS  +48h"  SK503  "SAS"               CPH   AMS   "$(t '+48 hours')"        DEPARTURE

echo ""
echo "=== ARRIVALs ==="

create "SK502  AMS→CPH  +20m"  SK502  "SAS"               AMS   CPH   "$(t '+20 minutes')"      ARRIVAL
create "DY442  OSL→CPH  +45m"  DY442  "Norwegian"         OSL   CPH   "$(t '+45 minutes')"      ARRIVAL
create "KL1204 AMS→CPH  +1h"   KL1204 "KLM"               AMS   CPH   "$(t '+1 hour')"          ARRIVAL
create "BA813  LHR→CPH  +1h30" BA813  "British Airways"   LHR   CPH   "$(t '+1 hour 30 minutes')" ARRIVAL
create "LH2935 FRA→CPH  +2h"   LH2935 "Lufthansa"         FRA   CPH   "$(t '+2 hours')"         ARRIVAL
create "TK1875 IST→CPH  +3h"   TK1875 "Turkish Airlines"  IST   CPH   "$(t '+3 hours')"         ARRIVAL
create "AF1284 CDG→CPH  +4h"   AF1284 "Air France"        CDG   CPH   "$(t '+4 hours')"         ARRIVAL
create "EK152  DXB→CPH  +6h"   EK152  "Emirates"          DXB   CPH   "$(t '+6 hours')"         ARRIVAL
create "FR4422 STN→CPH  +8h"   FR4422 "Ryanair"           STN   CPH   "$(t '+8 hours')"         ARRIVAL
create "SK948  JFK→CPH  +10h"  SK948  "SAS"               JFK   CPH   "$(t '+10 hours')"        ARRIVAL
create "SK246  NRT→CPH  +22h"  SK246  "SAS"               NRT   CPH   "$(t '+22 hours')"        ARRIVAL
create "DL8733 JFK→CPH  +25h"  DL8733 "Delta"             JFK   CPH   "$(t '+25 hours')"        ARRIVAL

echo ""
echo "Done — $ok created, $fail failed."
echo ""
