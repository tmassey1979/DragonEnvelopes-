# Issue #189 - Settings Profile and Budget Preferences Management

## Delivered
- Replaced Settings placeholder profile card with live family settings forms:
  - Family Profile: name, currency code, time zone
  - Budget Preferences: pay frequency, budgeting style, household monthly income
- Added explicit save actions from Settings:
  - `Save Profile`
  - `Save Budget`
- Added load behavior on Settings refresh/open for both family profile and budget preferences.
- Added validation and status/error feedback in Settings:
  - required fields and option validation
  - numeric validation for household monthly income
  - save/load summaries with timestamps and last operation status
- Added dedicated desktop family settings data service:
  - `IFamilySettingsDataService`
  - `FamilySettingsDataService`
- Wired settings route to use new service in `RouteRegistry`.
- Added desktop tests for load + save behavior:
  - profile/budget load
  - profile save
  - budget save
  - invalid income validation

## Validation
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj --filter "SettingsViewModelTests" -v minimal`
- `dotnet test tests/DragonEnvelopes.Desktop.Tests/DragonEnvelopes.Desktop.Tests.csproj -v minimal`
- `dotnet test DragonEnvelopes.sln -v minimal`
- Result: pass
