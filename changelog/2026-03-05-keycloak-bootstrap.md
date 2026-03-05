# 2026-03-05 - Keycloak Realm Bootstrap Assets

## Summary

Added repeatable Keycloak bootstrap configuration so local auth setup is automatic and consistent when starting Docker Compose.

## Completed Story

- #49 Add Keycloak realm/client bootstrap assets for local setup

## Key Changes

- Updated [docker-compose.yml](../docker-compose.yml):
  - Keycloak now starts with `--import-realm`.
  - Added import mount: `./infrastructure/keycloak/import:/opt/keycloak/data/import:ro`.
  - Switched to non-deprecated bootstrap admin env vars:
    - `KC_BOOTSTRAP_ADMIN_USERNAME`
    - `KC_BOOTSTRAP_ADMIN_PASSWORD`
- Added realm import file:
  - [infrastructure/keycloak/import/dragonenvelopes-realm.json](../infrastructure/keycloak/import/dragonenvelopes-realm.json)
- Added required realm roles:
  - `Parent`, `Adult`, `Teen`, `Child`
- Added client bootstrap config:
  - `dragonenvelopes-api`
  - `dragonenvelopes-desktop`
- Updated [.env.example](../.env.example) and [README.md](../README.md) bootstrap docs.

## Validation

- `docker compose up -d --build`
- Keycloak admin API checks verified:
  - realm `dragonenvelopes` exists
  - required roles exist
  - required clients exist
- `docker compose down`

