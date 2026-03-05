# 2026-03-05 - JWT Authentication and Role-Based Authorization Policies

## Summary

Integrated JWT bearer authentication against Keycloak and added explicit role-based authorization policies for family member roles.

## Completed Story

- #48 Wire JWT authentication and role-based authorization policies

## Key Changes

- Added JWT bearer package reference:
  - `src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj`
- Added auth policy constants and role set:
  - `src/DragonEnvelopes.Api/CrossCutting/Auth/ApiAuthorizationPolicies.cs`
- Added Keycloak role claims transformer:
  - Maps roles from `realm_access.roles` and `resource_access.{audience}.roles`
  - Adds normalized role claims for policy evaluation
  - `src/DragonEnvelopes.Api/CrossCutting/Auth/KeycloakRoleClaimsTransformer.cs`
- Updated API startup:
  - Configures `JwtBearer` with `Authentication:Authority` and `Authentication:Audience`
  - Configures explicit policies:
    - `ParentPolicy`
    - `AdultPolicy`
    - `TeenPolicy`
    - `ChildPolicy`
    - `ParentOrAdultPolicy`
    - `TeenOrAbovePolicy`
    - `AnyFamilyMemberPolicy`
  - Enables authentication/authorization middleware
  - Adds protected probe endpoints:
    - `GET /auth/me` (`AnyFamilyMemberPolicy`)
    - `GET /auth/parent-only` (`ParentPolicy`)
  - Keeps health/weather endpoints anonymous
  - `src/DragonEnvelopes.Api/Program.cs`
- Added default auth config keys:
  - `src/DragonEnvelopes.Api/appsettings.json`
  - `src/DragonEnvelopes.Api/appsettings.Development.json`
- Documented auth and policy setup:
  - `README.md`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
