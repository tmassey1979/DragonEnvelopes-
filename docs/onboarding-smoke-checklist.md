# Onboarding Smoke Checklist

## Preconditions
- API running with latest migrations applied.
- Desktop app built from current `main`.
- Test family with parent login available.

## Happy Path
1. Sign in as parent user.
2. Open `Onboarding` route.
3. Verify onboarding status loads and first incomplete step is selected.
4. Add one account row and one envelope row.
5. Set starter budget month and income.
6. Click `Submit Setup`.
7. Verify success status message appears.
8. Verify onboarding milestones are marked complete.

## Resume Path
1. Refresh app or sign out/sign in.
2. Open `Onboarding` route.
3. Verify wizard resumes using persisted onboarding profile state.

## Negative Path
1. Enter duplicate account names in onboarding form.
2. Submit setup.
3. Verify validation/error is surfaced and no partial data is created.

## Cross-Family Isolation Check
1. Use user A token and target family B id via API call.
2. Verify onboarding update/bootstrap returns `403 Forbidden`.
