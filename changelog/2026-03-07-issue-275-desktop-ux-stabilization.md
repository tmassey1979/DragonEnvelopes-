# Issue 275 - Desktop UX Stabilization

## Summary
Stabilized desktop UX around notifications, family-context loading, onboarding entry guardrails, and main-view readability.

## Delivered
- Fixed family selection deserialization path to use web JSON options and preserve real family IDs (prevents `Guid.Empty` context).
- Added fallback handling when family payload IDs deserialize empty.
- Added explicit no-family status messaging in main view model.
- Added login guardrail to block entering main shell when no family memberships are available.
- Improved notification center behavior:
  - exposed `HasToasts` contract,
  - added individual toast dismiss command wiring,
  - deduplicated repeated toasts,
  - reduced toast stack cap,
  - made error toasts transient by default,
  - clear action now clears full toast list.
- Moved notification panel to bottom-right with bounded scroll to reduce UI obstruction.
- Removed dark-theme indicator from top bar.
- Updated main shell visual treatment to light surface with red accents.
- Removed automatic scenario execution at view model construction/refresh to reduce startup noise and false errors.
- Added regression test validating family context resolution from camelCase API payloads.
- Fixed gateway container healthcheck probe to use `127.0.0.1` to avoid false unhealthy status.

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -c Release`
- `docker compose config`
- `docker compose up -d api-gateway`
- `curl.exe -sS -o NUL -w "%{http_code}" http://localhost:18088/health/ready`
- `curl.exe -sS -o NUL -w "%{http_code}" http://localhost:18088/api/v1/system/health`

## Time Spent
- 1h 20m
