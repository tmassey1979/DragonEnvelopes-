# Issue #201 - Family API Program Bootstrap Extraction

## Delivered
- Reduced `src/DragonEnvelopes.Family.Api/Program.cs` to orchestration-only startup flow.
- Added family bootstrap module split by concern:
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.cs`
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.Observability.cs`
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.Auth.cs`
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.Services.cs`
  - `src/DragonEnvelopes.Family.Api/Bootstrap/FamilyApiBootstrap.Runtime.cs`
- Extracted startup concerns from `Program.cs` into bootstrap methods:
  - Serilog/OpenAPI/API versioning
  - authentication and authorization policies
  - dependency injection + health checks
  - database migration startup execution
  - middleware/health/endpoint mapping
  - issuer and token intent helper methods
  - keycloak admin options builder
- Preserved existing endpoint behavior (`MapFamilyEndpoints`) and runtime wiring.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
