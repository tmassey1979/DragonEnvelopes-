# 2026-03-06 - Family Member Management Workspace

## Summary
Implemented a dedicated desktop family-member management workspace with member list and add-member form tied to family APIs.

## Changes
- Added family members API data service and interface:
  - `GET /families/{familyId}/members`
  - `POST /families/{familyId}/members`
- Added `FamilyMembersViewModel` with:
  - loading/error/empty states
  - add-member validation (keycloak id, name, email, role)
  - reset/add/list refresh workflow
- Added `FamilyMemberItemViewModel`.
- Added new navigation route `/family-members`.
- Added full XAML template: member grid + add-member editor panel.

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj -p:OutputPath=bin/TempVerify/` (pass)

## Notes
- Role options align with backend validator: Parent, Adult, Teen, Child.
- Uses active family from `IFamilyContext`.
