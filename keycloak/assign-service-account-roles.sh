#!/bin/sh
# Assigns realm-management roles to the airport-api service account.
# Runs once after Keycloak is healthy.

KC_URL="${KC_URL:-http://localhost:8080}"
REALM="airport-system"
ADMIN_USER="admin"
ADMIN_PASS="admin"
CLIENT_ID="airport-api"

# Get admin token
TOKEN=$(curl -sf -X POST "$KC_URL/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=admin-cli&username=$ADMIN_USER&password=$ADMIN_PASS" \
  | sed 's/.*"access_token":"\([^"]*\)".*/\1/')

# Get airport-api client UUID
CLIENT_UUID=$(curl -sf "$KC_URL/admin/realms/$REALM/clients?clientId=$CLIENT_ID" \
  -H "Authorization: Bearer $TOKEN" \
  | sed 's/.*"id":"\([^"]*\)".*/\1/' | head -1)

# Get service account user ID
SA_USER_ID=$(curl -sf "$KC_URL/admin/realms/$REALM/clients/$CLIENT_UUID/service-account-user" \
  -H "Authorization: Bearer $TOKEN" \
  | sed 's/.*"id":"\([^"]*\)".*/\1/')

# Get realm-management client UUID
RM_UUID=$(curl -sf "$KC_URL/admin/realms/$REALM/clients?clientId=realm-management" \
  -H "Authorization: Bearer $TOKEN" \
  | sed 's/.*"id":"\([^"]*\)".*/\1/' | head -1)

# Get individual role representations for the four roles we need
get_role() {
  curl -sf "$KC_URL/admin/realms/$REALM/clients/$RM_UUID/roles/$1" \
    -H "Authorization: Bearer $TOKEN"
}

R1=$(get_role "manage-users")
R2=$(get_role "query-users")
R3=$(get_role "view-users")
R4=$(get_role "manage-realm")
ROLES="[$R1,$R2,$R3,$R4]"

# Assign roles
curl -sf -X POST \
  "$KC_URL/admin/realms/$REALM/users/$SA_USER_ID/role-mappings/clients/$RM_UUID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$ROLES"

echo "Service account roles assigned."
