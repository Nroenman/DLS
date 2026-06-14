#!/bin/sh
set -e

IMPORT_DIR=/opt/keycloak/data/import
mkdir -p "$IMPORT_DIR"

# Substitute Google OAuth credentials from environment variables into the realm
# template before Keycloak imports it. Keycloak's --import-realm does not do
# environment variable substitution in realm JSON files on its own.
sed \
  -e "s|\${GOOGLE_CLIENT_ID}|${GOOGLE_CLIENT_ID}|g" \
  -e "s|\${GOOGLE_SECRET}|${GOOGLE_SECRET}|g" \
  /realm-init/airport-realm.json.template \
  > "$IMPORT_DIR/airport-realm.json"

exec /opt/keycloak/bin/kc.sh start-dev --import-realm --health-enabled=true
