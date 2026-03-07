# Issue #200 - ApiBootstrap Concern-Focused Modules

## Delivered
- Split API bootstrap configuration into concern-focused partial files:
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.Observability.cs`
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.Auth.cs`
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.Services.cs`
  - `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.Runtime.cs`
- Reduced `src/DragonEnvelopes.Api/Bootstrap/ApiBootstrap.cs` to the partial class entrypoint shell.
- Preserved existing startup behavior while improving separation of concerns across:
  - logging/openapi/versioning
  - authentication/authorization
  - service registration/health checks/options builders
  - runtime middleware/migrations/endpoint mapping
- Restored required namespace imports introduced by split:
  - `Serilog` for `UseSerilogRequestLogging`
  - `DragonEnvelopes.ProviderClients` for `AddProviderClients`

## Validation
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
