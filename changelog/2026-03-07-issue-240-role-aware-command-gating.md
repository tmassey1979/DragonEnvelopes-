# Issue 240 - Role-Aware Command Gating Audit

## Summary
Implemented a role-aware gating pass for desktop navigation and restricted commands, with explicit Parent/Admin handling and test coverage across Parent vs Adult/Teen/Child contexts.

## Delivered
- Added role-aware workspace contract:
  - `client/DragonEnvelopes.Desktop/ViewModels/IRoleAwareWorkspaceViewModel.cs`
- Main window role gate hardening:
  - apply role gates at startup, session restore failure, sign-out, and auth refresh failures
  - elevated role detection now accepts `Parent`, `Admin`, `Administrator`
  - propagate role context into workspace view models that implement the role-aware contract
- Recurring bills command gating:
  - `RunAutoPostNowCommand` now requires elevated role (`CanRunAutoPostNow`)
  - command uses CanExecute gating and guard clause validation
  - recurring template button now binds to `CanRunAutoPostNow`
- Settings capability matrix updates:
  - invite timeline marked available
  - role/command gating matrix marked available with documentation reference
- Documentation:
  - `docs/qa/desktop-role-command-matrix.md`
- Tests:
  - added role-context tests in `MainWindowViewModelTests` for Parent/Admin and Adult/Teen/Child behaviors

## Validation
- `dotnet build DragonEnvelopes.sln -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal --no-build`
