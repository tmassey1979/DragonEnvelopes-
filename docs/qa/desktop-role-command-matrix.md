# Desktop Role Command Matrix

This matrix documents command and route gating in the WPF client.
Server authorization remains the source of truth.

## Role Model
- Elevated roles: `Parent`, `Admin`, `Administrator`
- Standard roles: `Adult`, `Teen`, `Child`

## Route Gating
| Area | Route Key | Required Role | Enforcement |
| --- | --- | --- | --- |
| Financial Integrations | `/financial-integrations` | Elevated | Disabled in sidebar for standard roles |
| Family Members | `/family-members` | Elevated | Disabled in sidebar for standard roles |
| Onboarding Wizard | `/onboarding` | Elevated | Disabled in sidebar for standard roles |

## Command Gating
| Workspace | Command | Required Role | UX Behavior |
| --- | --- | --- | --- |
| Recurring Bills | `RunAutoPostNowCommand` | Elevated | Command disabled for standard roles and explicit validation message if invoked |

## Commands Available To Any Family Member
The following workspace commands remain available for `Adult`, `Teen`, and `Child` because the backing APIs use `AnyFamilyMember` authorization:
- Dashboard refresh
- Accounts create/list
- Envelopes CRUD/archive
- Transactions CRUD/delete/restore/split
- Budgets create/update
- Automation rules CRUD
- Recurring bill CRUD/projection/history export
- Imports preview/commit
- Reports queries
- Settings profile/budget/session tools

If server-side policies change for any endpoint, update this matrix and the corresponding desktop command gates in the same PR.
