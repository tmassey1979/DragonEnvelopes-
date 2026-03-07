# Issue #202 - Ledger API Program Bootstrap Extraction

## Delivered
- Reduced `src/DragonEnvelopes.Ledger.Api/Program.cs` to orchestration-only startup flow.
- Added ledger bootstrap module split by concern:
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.cs`
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.Observability.cs`
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.Auth.cs`
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.Services.cs`
  - `src/DragonEnvelopes.Ledger.Api/Bootstrap/LedgerApiBootstrap.Runtime.cs`
- Extracted startup concerns from `Program.cs` into bootstrap methods:
  - Serilog/OpenAPI/API versioning
  - authentication and authorization policies
  - dependency injection + health checks
  - database migration startup execution
  - middleware/health/endpoint mapping
  - issuer and token intent helper methods
- Preserved existing endpoint behavior (`MapAutomationEndpoints`, `MapAccountAndTransactionEndpoints`) and runtime wiring.

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
