# Issue #199 - Program Startup Bootstrap Extraction

## Delivered
- Reduced `Program.cs` to orchestration-focused startup flow.
- Added new bootstrap module:
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.cs`
- Extracted startup concerns from `Program.cs` into `ApiBootstrap`:
  - Serilog/Loki configuration
  - OpenAPI + API versioning setup
  - Authentication + authorization policy setup
  - Dependency registration + hosted workers
  - Health checks registration
  - DB migration/ensure-created startup step
  - Middleware pipeline setup
  - Health endpoint mapping
  - Versioned endpoint mapping
  - Issuer and token audience/azp validation helpers
  - Keycloak/Stripe/DataRetention options builders
- Preserved endpoint mapping behavior and runtime wiring.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
