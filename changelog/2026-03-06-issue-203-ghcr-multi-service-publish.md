# Issue #203 - GHCR Multi-Service Publish

## Delivered
- Updated `.github/workflows/docker-publish.yml` to publish multiple service images via a matrix job.
- Added publish targets for:
  - `ghcr.io/<owner>/dragonenvelopes-api`
  - `ghcr.io/<owner>/dragonenvelopes-family-api`
  - `ghcr.io/<owner>/dragonenvelopes-ledger-api`
- Wired each matrix entry to the correct Dockerfile:
  - `src/DragonEnvelopes.Api/Dockerfile`
  - `src/DragonEnvelopes.Family.Api/Dockerfile`
  - `src/DragonEnvelopes.Ledger.Api/Dockerfile`
- Preserved existing workflow trigger conditions and Docker tag strategy (`branch`, `tag`, `sha`).

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
