#!/usr/bin/env bash
set -euo pipefail

API_GATEWAY_BASE_URL="${API_GATEWAY_BASE_URL:-http://localhost:18088}"

request() {
  local method="$1"
  local path="$2"
  local payload="${3:-}"

  local url="${API_GATEWAY_BASE_URL}${path}"
  local body_file
  body_file="$(mktemp)"
  local status

  if [[ -n "${payload}" ]]; then
    status="$(curl -sS -o "${body_file}" -w "%{http_code}" -X "${method}" "${url}" -H "Content-Type: application/json" --data "${payload}")"
  else
    status="$(curl -sS -o "${body_file}" -w "%{http_code}" -X "${method}" "${url}")"
  fi

  echo "${status}" "${body_file}"
}

assert_status_in() {
  local status="$1"
  local body_file="$2"
  local operation="$3"
  shift 3

  for expected in "$@"; do
    if [[ "${status}" == "${expected}" ]]; then
      rm -f "${body_file}"
      return 0
    fi
  done

  echo "[gateway-route-smoke] ${operation} returned HTTP ${status}; expected one of: $*" >&2
  echo "[gateway-route-smoke] response:" >&2
  cat "${body_file}" >&2 || true
  rm -f "${body_file}"
  exit 1
}

echo "[gateway-route-smoke] Validating routed endpoints via ${API_GATEWAY_BASE_URL}"

read -r status body_file < <(request "POST" "/api/v1/families/onboard" "{}")
assert_status_in "${status}" "${body_file}" "family onboard route" "400" "422"

read -r status body_file < <(request "POST" "/api/v1/imports/transactions/preview" "{}")
assert_status_in "${status}" "${body_file}" "ledger import preview route" "400" "401" "403"

read -r status body_file < <(request "GET" "/api/v1/accounts?familyId=00000000-0000-0000-0000-000000000001")
assert_status_in "${status}" "${body_file}" "ledger accounts route" "401" "403"

read -r status body_file < <(request "POST" "/api/v1/webhooks/stripe" '{"id":"evt_gateway_route_smoke","type":"issuing_authorization.request"}')
assert_status_in "${status}" "${body_file}" "financial stripe webhook route" "401"

read -r status body_file < <(request "GET" "/api/v1/reports/envelope-balances?familyId=00000000-0000-0000-0000-000000000001")
assert_status_in "${status}" "${body_file}" "ledger report route" "401" "403"

echo "[gateway-route-smoke] Route ownership checks passed."
