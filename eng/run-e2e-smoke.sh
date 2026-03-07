#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${API_BASE_URL:-http://localhost:18088/api/v1}"
KEYCLOAK_BASE_URL="${KEYCLOAK_BASE_URL:-http://localhost:18080}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-dragonenvelopes}"
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-dragonenvelopes-api}"
ARTIFACT_DIR="${ARTIFACT_DIR:-artifacts/e2e-smoke}"

mkdir -p "${ARTIFACT_DIR}"

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required for the e2e smoke script." >&2
  exit 1
fi

SUMMARY_FILE="${ARTIFACT_DIR}/e2e-smoke-summary.md"
LAST_STATUS=""
LAST_BODY_FILE=""
LAST_HEADERS_FILE=""
REQUEST_INDEX=0

log_step() {
  echo "[e2e-smoke] $1"
}

next_request_prefix() {
  REQUEST_INDEX=$((REQUEST_INDEX + 1))
  printf "%02d" "${REQUEST_INDEX}"
}

fail() {
  local message="$1"
  echo "[e2e-smoke] ERROR: ${message}" >&2
  cat > "${SUMMARY_FILE}" <<EOF
# E2E Smoke Summary

- Result: FAILED
- Failure: ${message}
- API base: ${API_BASE_URL}
- Keycloak base: ${KEYCLOAK_BASE_URL}
EOF
  exit 1
}

assert_status() {
  local actual_status="$1"
  local expected_status="$2"
  local operation="$3"
  if [[ "${actual_status}" != "${expected_status}" ]]; then
    fail "${operation} returned HTTP ${actual_status}; expected ${expected_status}. Response file: ${LAST_BODY_FILE}"
  fi
}

request_json() {
  local name="$1"
  local method="$2"
  local url="$3"
  local token="$4"
  local payload="${5:-}"
  local prefix
  prefix="$(next_request_prefix)"

  LAST_BODY_FILE="${ARTIFACT_DIR}/${prefix}-${name}.json"
  LAST_HEADERS_FILE="${ARTIFACT_DIR}/${prefix}-${name}.headers.txt"

  local -a curl_args=(
    -sS
    -X "${method}"
    "${url}"
    -D "${LAST_HEADERS_FILE}"
    -o "${LAST_BODY_FILE}"
    -w "%{http_code}"
  )

  if [[ -n "${token}" ]]; then
    curl_args+=(-H "Authorization: Bearer ${token}")
  fi

  if [[ -n "${payload}" ]]; then
    curl_args+=(-H "Content-Type: application/json" --data "${payload}")
  fi

  LAST_STATUS="$(curl "${curl_args[@]}")"
}

request_form() {
  local name="$1"
  local url="$2"
  shift 2
  local prefix
  prefix="$(next_request_prefix)"

  LAST_BODY_FILE="${ARTIFACT_DIR}/${prefix}-${name}.json"
  LAST_HEADERS_FILE="${ARTIFACT_DIR}/${prefix}-${name}.headers.txt"

  local -a curl_args=(
    -sS
    -X POST
    "${url}"
    -D "${LAST_HEADERS_FILE}"
    -o "${LAST_BODY_FILE}"
    -w "%{http_code}"
    -H "Content-Type: application/x-www-form-urlencoded"
  )

  local field
  for field in "$@"; do
    curl_args+=(--data-urlencode "${field}")
  done

  LAST_STATUS="$(curl "${curl_args[@]}")"
}

SUFFIX="$(date +%s)-${RANDOM}"
FAMILY_NAME="Smoke Family ${SUFFIX}"
GUARDIAN_FIRST_NAME="Smoke"
GUARDIAN_LAST_NAME="Owner"
GUARDIAN_EMAIL="smoke-${SUFFIX}@test.dev"
GUARDIAN_PASSWORD="SmokePass!${SUFFIX}"

log_step "Onboarding smoke family."
ONBOARD_PAYLOAD="$(
  jq -nc \
    --arg familyName "${FAMILY_NAME}" \
    --arg firstName "${GUARDIAN_FIRST_NAME}" \
    --arg lastName "${GUARDIAN_LAST_NAME}" \
    --arg email "${GUARDIAN_EMAIL}" \
    --arg password "${GUARDIAN_PASSWORD}" \
    '{
      familyName: $familyName,
      primaryGuardianFirstName: $firstName,
      primaryGuardianLastName: $lastName,
      email: $email,
      password: $password
    }'
)"
request_json "family-onboard" "POST" "${API_BASE_URL}/families/onboard" "" "${ONBOARD_PAYLOAD}"
assert_status "${LAST_STATUS}" "201" "POST /families/onboard"
FAMILY_ID="$(jq -r '.id // empty' "${LAST_BODY_FILE}")"
if [[ -z "${FAMILY_ID}" ]]; then
  fail "Onboard response did not contain a family id."
fi

log_step "Requesting access token using password grant."
request_form \
  "token-password" \
  "${KEYCLOAK_BASE_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token" \
  "grant_type=password" \
  "client_id=${KEYCLOAK_CLIENT_ID}" \
  "username=${GUARDIAN_EMAIL}" \
  "password=${GUARDIAN_PASSWORD}"
assert_status "${LAST_STATUS}" "200" "Keycloak password token request"
ACCESS_TOKEN="$(jq -r '.access_token // empty' "${LAST_BODY_FILE}")"
REFRESH_TOKEN="$(jq -r '.refresh_token // empty' "${LAST_BODY_FILE}")"
if [[ -z "${ACCESS_TOKEN}" || -z "${REFRESH_TOKEN}" ]]; then
  fail "Token response is missing access_token or refresh_token."
fi

log_step "Validating authenticated session via /auth/me."
request_json "auth-me-initial" "GET" "${API_BASE_URL}/auth/me" "${ACCESS_TOKEN}"
assert_status "${LAST_STATUS}" "200" "GET /auth/me (initial)"
if ! jq -e --arg familyId "${FAMILY_ID}" '.familyIds | map(tostring) | index($familyId) != null' "${LAST_BODY_FILE}" >/dev/null; then
  fail "Initial /auth/me response did not include the onboarded family id."
fi

log_step "Refreshing token to validate session restore."
request_form \
  "token-refresh" \
  "${KEYCLOAK_BASE_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token" \
  "grant_type=refresh_token" \
  "client_id=${KEYCLOAK_CLIENT_ID}" \
  "refresh_token=${REFRESH_TOKEN}"
assert_status "${LAST_STATUS}" "200" "Keycloak refresh token request"
RESTORED_ACCESS_TOKEN="$(jq -r '.access_token // empty' "${LAST_BODY_FILE}")"
if [[ -z "${RESTORED_ACCESS_TOKEN}" ]]; then
  fail "Refresh token response did not contain access_token."
fi

request_json "auth-me-restored" "GET" "${API_BASE_URL}/auth/me" "${RESTORED_ACCESS_TOKEN}"
assert_status "${LAST_STATUS}" "200" "GET /auth/me (restored session)"

log_step "Reading family details and members."
request_json "family-get" "GET" "${API_BASE_URL}/families/${FAMILY_ID}" "${RESTORED_ACCESS_TOKEN}"
assert_status "${LAST_STATUS}" "200" "GET /families/{familyId}"
request_json "family-members-list" "GET" "${API_BASE_URL}/families/${FAMILY_ID}/members" "${RESTORED_ACCESS_TOKEN}"
assert_status "${LAST_STATUS}" "200" "GET /families/{familyId}/members"

log_step "Creating account for import."
ACCOUNT_PAYLOAD="$(
  jq -nc \
    --arg familyId "${FAMILY_ID}" \
    --arg name "Smoke Checking" \
    --arg type "Checking" \
    --argjson openingBalance 1000 \
    '{
      familyId: $familyId,
      name: $name,
      type: $type,
      openingBalance: $openingBalance
    }'
)"
request_json "account-create" "POST" "${API_BASE_URL}/accounts" "${RESTORED_ACCESS_TOKEN}" "${ACCOUNT_PAYLOAD}"
assert_status "${LAST_STATUS}" "201" "POST /accounts"
ACCOUNT_ID="$(jq -r '.id // empty' "${LAST_BODY_FILE}")"
if [[ -z "${ACCOUNT_ID}" ]]; then
  fail "Create account response did not contain an account id."
fi

CSV_CONTENT=$'date,amount,merchant,description,category\n2026-01-05,-42.50,Grocer,Weekly groceries,Food\n2026-01-10,2200.00,Employer,Paycheck,Income'

log_step "Previewing transaction import."
IMPORT_PREVIEW_PAYLOAD="$(
  jq -nc \
    --arg familyId "${FAMILY_ID}" \
    --arg accountId "${ACCOUNT_ID}" \
    --arg csvContent "${CSV_CONTENT}" \
    --arg delimiter "," \
    '{
      familyId: $familyId,
      accountId: $accountId,
      csvContent: $csvContent,
      delimiter: $delimiter,
      headerMappings: null
    }'
)"
request_json "import-preview" "POST" "${API_BASE_URL}/imports/transactions/preview" "${RESTORED_ACCESS_TOKEN}" "${IMPORT_PREVIEW_PAYLOAD}"
assert_status "${LAST_STATUS}" "200" "POST /imports/transactions/preview"
ACCEPTED_ROWS_JSON="$(jq -c '[.rows[] | select((.errors | length) == 0 and (.isDuplicate | not)) | .rowNumber]' "${LAST_BODY_FILE}")"
if [[ "${ACCEPTED_ROWS_JSON}" == "[]" ]]; then
  fail "Import preview did not produce accepted row numbers."
fi

log_step "Committing transaction import."
IMPORT_COMMIT_PAYLOAD="$(
  jq -nc \
    --arg familyId "${FAMILY_ID}" \
    --arg accountId "${ACCOUNT_ID}" \
    --arg csvContent "${CSV_CONTENT}" \
    --arg delimiter "," \
    --argjson acceptedRows "${ACCEPTED_ROWS_JSON}" \
    '{
      familyId: $familyId,
      accountId: $accountId,
      csvContent: $csvContent,
      delimiter: $delimiter,
      headerMappings: null,
      acceptedRowNumbers: $acceptedRows
    }'
)"
request_json "import-commit" "POST" "${API_BASE_URL}/imports/transactions/commit" "${RESTORED_ACCESS_TOKEN}" "${IMPORT_COMMIT_PAYLOAD}"
assert_status "${LAST_STATUS}" "200" "POST /imports/transactions/commit"
INSERTED_COUNT="$(jq -r '.inserted // 0' "${LAST_BODY_FILE}")"
if [[ "${INSERTED_COUNT}" -lt 1 ]]; then
  fail "Import commit inserted ${INSERTED_COUNT} rows; expected at least one inserted row."
fi

log_step "Reading envelope balances report."
request_json "report-envelope-balances" "GET" "${API_BASE_URL}/reports/envelope-balances?familyId=${FAMILY_ID}" "${RESTORED_ACCESS_TOKEN}"
assert_status "${LAST_STATUS}" "200" "GET /reports/envelope-balances?familyId={familyId}"

cat > "${SUMMARY_FILE}" <<EOF
# E2E Smoke Summary

- Result: SUCCESS
- API base: ${API_BASE_URL}
- Keycloak base: ${KEYCLOAK_BASE_URL}
- Family id: ${FAMILY_ID}
- Account id: ${ACCOUNT_ID}
- Imported rows inserted: ${INSERTED_COUNT}
EOF

log_step "Smoke scenario completed successfully."
