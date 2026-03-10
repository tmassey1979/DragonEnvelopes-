# Dragon Envelopes

## Complete Onboarding Wizard Codex

This document defines the **complete onboarding experience** for Dragon Envelopes.

The onboarding wizard replaces the previous "Create Family" screen and guides users through:

1. Account creation
2. Family setup
3. Member invitations
4. Budget configuration
5. Bank connection (Plaid)
6. Transaction analysis
7. Envelope creation
8. Stripe financial accounts
9. Debit card provisioning
10. Automation rules
11. Dashboard activation

Architecture targets:

* WPF Desktop Client
* ASP.NET Core API
* PostgreSQL
* Keycloak Authentication
* Docker backend deployment

Design goals:

* Extremely simple onboarding
* Minimal screens
* Dark modern UI
* Financial clarity
* Automation-first setup

---

# Wizard Overview

```
Welcome
↓
Create Account
↓
Create Family
↓
Add Family Members
↓
Budget Preferences
↓
Connect Bank (Plaid)
↓
Import Transactions
↓
Categorize Spending
↓
Auto Generate Envelopes
↓
Create Stripe Envelope Accounts
↓
Create Debit Cards
↓
Setup Automation Rules
↓
Finish
↓
Dashboard
```

---

# Wizard Architecture

## WPF Project Structure

```
/Views
    WelcomeView.xaml
    RegisterView.xaml
    CreateFamilyView.xaml
    AddMembersView.xaml
    BudgetPreferencesView.xaml
    ConnectBankView.xaml
    ImportTransactionsView.xaml
    CategorizeTransactionsView.xaml
    EnvelopeGenerationView.xaml
    StripeAccountsView.xaml
    DebitCardsView.xaml
    AutomationRulesView.xaml
    FinishView.xaml

/ViewModels
    OnboardingViewModel.cs
    RegisterViewModel.cs
    FamilyViewModel.cs
    MembersViewModel.cs
    BudgetPreferencesViewModel.cs
    PlaidViewModel.cs
    TransactionImportViewModel.cs
    CategorizationViewModel.cs
    EnvelopeGenerationViewModel.cs
    StripeAccountsViewModel.cs
    DebitCardsViewModel.cs
    AutomationRulesViewModel.cs

/Services
    AuthService.cs
    FamilyService.cs
    MemberService.cs
    PlaidService.cs
    TransactionService.cs
    EnvelopeService.cs
    StripeService.cs

/Models
    Family.cs
    FamilyMember.cs
    Envelope.cs
    Transaction.cs
    FinancialAccount.cs
```

---

# Step 1 — Welcome Screen

Purpose:

Introduce the application.

UI

```
Welcome to Dragon Envelopes
```

Subtitle

```
Smart envelope budgeting for modern families.
```

Buttons

```
Get Started
Sign In
```

---

# Step 2 — Create Account

Creates identity using Keycloak.

Fields

```
Email
Password
Confirm Password
First Name
Last Name
```

API

```
POST /api/auth/register
```

Backend Actions

* Create Keycloak user
* Assign role FamilyOwner
* Return JWT token

---

# Step 3 — Create Family

Fields

```
Family Name
Currency
Timezone
```

Example

```
Massey Family
USD
Central Time
```

API

```
POST /api/families
```

Database

```
Families

FamilyId
Name
Currency
Timezone
CreatedByUserId
CreatedAt
```

---

# Step 4 — Add Family Members

Users can invite household members.

Roles

```
Owner
Adult
Teen
Child
Viewer
```

Fields

```
Name
Email
Role
```

API

```
POST /api/families/{familyId}/members
```

Behavior

* Invite email sent
* User completes account setup

---

# Step 5 — Budget Preferences

Fields

```
Pay Frequency
Monthly Household Income
Budget Style
```

Options

Pay Frequency

```
Weekly
Biweekly
Monthly
Irregular
```

Budget Style

```
Envelope Budget
Manual Budget
```

API

```
POST /api/families/{familyId}/preferences
```

---

# Step 6 — Connect Bank Accounts (Plaid)

Purpose

Import transaction history.

Flow

```
WPF Client
↓
Plaid Link
↓
User selects bank
↓
Public token returned
↓
Backend exchanges for access token
```

API

```
POST /api/plaid/connect
```

Stored Data

```
PlaidItemId
AccessToken
LinkedAccounts
```

---

# Step 7 — Import Transactions

Transactions imported from Plaid.

Typical import range

```
Last 6–12 months
```

API

```
POST /api/transactions/import
```

Database

```
Transactions

TransactionId
AccountId
Amount
Merchant
Category
Date
```

---

# Step 8 — Categorize Spending

AI-assisted categorization.

User verifies categories.

Example categories

```
Groceries
Dining
Utilities
Mortgage
Gas
Insurance
Clothing
Entertainment
Medical
```

User can correct categories.

Corrected categories improve classification.

---

# Step 9 — Auto Generate Envelopes

System analyzes spending patterns.

Creates envelopes automatically.

Example envelopes

```
Groceries
Mortgage
Utilities
Fuel
Dining
Kids Clothing
Medical
Streaming
Emergency Fund
```

Algorithm

```
Group transactions by category
Calculate monthly average
Create envelope per category
```

User can:

* rename envelopes
* merge envelopes
* delete envelopes

---

# Step 10 — Create Stripe Financial Accounts

Each envelope can optionally be backed by a Stripe Financial Account.

Architecture

```
Family
  → Envelope
       → Stripe Financial Account
```

Example

```
Groceries Envelope → Stripe Account
Mortgage Envelope → Stripe Account
Kids Clothing Envelope → Stripe Account
```

API

```
POST /api/stripe/accounts/create
```

Benefits

* real money segregation
* automated budgeting

---

# Step 11 — Create Debit Cards

Stripe can issue cards tied to envelopes.

Types

```
Virtual Cards
Physical Cards
```

Example

```
Groceries Card
Kids Allowance Card
Family Spending Card
```

API

```
POST /api/stripe/cards/create
```

Rules

```
Card spending limited to envelope balance
```

---

# Step 12 — Setup Automation Rules

Automation examples

```
Paycheck detected → distribute funds

Groceries envelope refill weekly

Move leftover dining funds → savings
```

Automation Types

```
Income Distribution
Recurring Funding
Auto Savings
Bill Protection
```

---

# Step 13 — Finish Screen

Summary

```
Accounts connected
Transactions analyzed
Envelopes created
Cards issued
Automation enabled
```

Button

```
Launch Dashboard
```

---

# Dashboard Redirect

User lands on

```
/dashboard
```

Modules

```
Envelope Balances
Recent Transactions
Budget Health
Upcoming Bills
Family Activity
```

---

# Database Core Tables

```
Users
Families
FamilyMembers
Accounts
Transactions
Envelopes
EnvelopeBalances
AutomationRules
StripeAccounts
DebitCards
```

All tables include

```
FamilyId
```

for multi‑tenant isolation.

---

# UI Design Requirements

Style

```
Dark
Minimal
Fintech aesthetic
```

Colors

```
Background #121212
Cards #1E1E1E
Accent #00C896
Text #FFFFFF
```

Layout

```
Centered wizard card
Max width 720px
Step indicator
Next / Back navigation
```

---

# Implementation Order

Recommended build order

```
1 Authentication
2 Family creation
3 Members
4 Budget preferences
5 Dashboard
6 Plaid integration
7 Transaction analysis
8 Envelope generation
9 Stripe accounts
10 Debit cards
11 Automation
```

---

# End of Document
