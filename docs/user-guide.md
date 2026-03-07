# User Guide

This guide is for families using DragonEnvelopes in the desktop app.

## 1. Sign In and Access

1. Launch `DragonEnvelopes.Desktop`.
2. Sign in with your account credentials.
3. If you are new, use the create-family/onboarding flow first.

Behavior notes:
- Main workspace loads after successful sign-in.
- Canceling login exits the app.

## 2. Create or Join a Family

Create family account flow captures:
- Family name
- Guardian first and last name
- Email and password

If you were invited:
- Use your invite onboarding link/token flow.
- Complete registration and return to desktop sign-in.

## 3. Member Roles

Family members can be assigned roles:
- Parent
- Adult
- Teen
- Child

Role impacts what actions are available (for example, admin-level financial configuration is usually Parent-restricted).

## 4. Connect Financial Providers

In Financial Integrations:
- Connect Plaid to sync accounts and transactions.
- Connect Stripe for envelope-backed financial accounts/cards.

You can also:
- Review provider activity health.
- View provider timeline events.
- Load event detail and replay failed supported events.

## 5. Accounts and Envelopes

Typical setup:
1. Add or sync accounts.
2. Create envelopes (for example Groceries, Rent, Utilities).
3. Set budget allocations.
4. Monitor envelope balances and spending.

## 6. Transactions

You can add transactions by:
- Plaid sync
- CSV import
- Manual entry

Available actions include:
- Categorization
- Envelope assignment
- Split transaction editing
- Soft-delete/restore (where enabled)

## 7. Recurring Bills and Automation

Recurring bills can be created and projected forward for planning.

Automation can apply rules for:
- Categorization
- Allocation behavior

Manual transaction splits override automatic allocation behavior.

## 8. Importing from CSV

Import workflow:
1. Preview CSV parse results.
2. Review validation errors and dedupe flags.
3. Commit accepted rows.

The import response reports parsed/valid/deduped/inserted/failed counts.

## 9. Troubleshooting

If login fails:
- Confirm Keycloak is reachable.
- Confirm desktop auth settings match environment.

If timeline/detail actions fail:
- Refresh provider timeline.
- Confirm event source and event id are populated.

If data seems missing:
- Verify selected family context.
- Verify your role has access to the operation.

## 10. Quick Support Checklist

When reporting an issue, include:
- What you clicked and expected.
- Exact error text.
- Approximate timestamp.
- Current family and role.
- Trace id shown in app status (if available).
