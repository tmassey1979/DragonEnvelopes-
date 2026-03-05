# 2026-03-05 - API Container Hardening

## Summary

Hardened the API container image by using a dedicated restore/publish/runtime multi-stage build and running the final container as a non-root user.

## Completed Story

- #86 Harden API container with multi-stage Dockerfile and non-root runtime

## Key Changes

- Updated API Dockerfile:
  - Split build into explicit `restore` and `publish` stages
  - Final runtime stage uses only published output
  - Added non-root runtime user `dragonenvelopes` (UID/GID `10001`)
  - Set container process to run as non-root via `USER dragonenvelopes`
  - Added runtime env hardening (`ASPNETCORE_HTTP_PORTS`, `DOTNET_EnableDiagnostics`)
  - `src/DragonEnvelopes.Api/Dockerfile`
- Updated documentation:
  - Added API container runtime notes, runtime port, and required env vars
  - `README.md`

## Validation

- `docker compose up -d --build api`
- `docker compose ps api` shows API container healthy
- `docker compose exec -T api id` confirms non-root runtime (`uid=10001`, `gid=10001`)
