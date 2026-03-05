# 2026-03-05 - Automation Rule Schema and CRUD APIs (#76)

## Summary
- Added automation rule domain model and rule-type enum for family-scoped categorization/allocation automation.
- Added EF Core configuration, DbContext wiring, repository implementation, and `AddAutomationRules` migration.
- Added automation contracts, application DTOs/interfaces/service, and API endpoints under `/api/v1/automation/rules`.
- Added FluentValidation validators for create/update automation-rule requests.
- Added application tests covering create, invalid JSON rejection, list ordering, and delete-not-found behavior.
- Updated API endpoint list in README.

## Endpoints
- `POST /api/v1/automation/rules`
- `GET /api/v1/automation/rules?familyId={familyId}&type={type?}&enabled={bool?}`
- `GET /api/v1/automation/rules/{ruleId}`
- `PUT /api/v1/automation/rules/{ruleId}`
- `POST /api/v1/automation/rules/{ruleId}/enable`
- `POST /api/v1/automation/rules/{ruleId}/disable`
- `DELETE /api/v1/automation/rules/{ruleId}`

## Validation
- `dotnet build src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj --configuration Release`
- `dotnet test tests/DragonEnvelopes.Application.Tests/DragonEnvelopes.Application.Tests.csproj --configuration Release`
- Manual API smoke: create/list/update/disable/delete completed successfully against local API container.
