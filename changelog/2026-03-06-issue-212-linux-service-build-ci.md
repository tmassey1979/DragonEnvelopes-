# Issue #212 - Linux Service Build CI Coverage

## Delivered
- Added `linux-service-build` job to `.github/workflows/ci.yml` on `ubuntu-latest`.
- Job restores and builds split API services in Release configuration:
  - `DragonEnvelopes.Api`
  - `DragonEnvelopes.Family.Api`
  - `DragonEnvelopes.Ledger.Api`
- Kept existing Windows build/test flow unchanged.
- Avoided desktop/WPF Linux build constraints by scoping to service projects only.

## Validation
- Local restore/build parity commands:
  - `dotnet restore src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
  - `dotnet restore src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj`
  - `dotnet restore src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj`
  - `dotnet build ... --configuration Release --no-restore` (all three projects)
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
