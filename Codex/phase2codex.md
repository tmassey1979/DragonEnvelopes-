Dragon Envelopes – Complete Codex Architecture
Core Philosophy

The system is designed around real-world financial processes:

Family joins
→ Members join
→ Accounts connected
→ Transactions imported
→ Budget envelopes created
→ Paycheck distributed
→ Spending tracked
→ Insights generated

Phase 1:
Working budgeting system.

Phase 2:
Real fintech integration (Stripe envelope accounts + cards).

Phase 1 – Infrastructure Codex
Goal

Generate a fully runnable local environment with Docker.

Includes:

Backend API
Postgres
Keycloak
pgAdmin
WPF Client
Docker Architecture
+-----------------------+
| WPF Desktop Client    |
+-----------+-----------+
            |
            v
+-----------------------+
| ASP.NET Core API      |
+-----------+-----------+
            |
   -------------------------
   |                       |
   v                       v
PostgreSQL             Keycloak
   |
pgAdmin4
Containers
API
.NET 8
ASP.NET Core Web API
EF Core
Database
PostgreSQL
Authentication
Keycloak
OIDC
JWT
Admin
pgAdmin4
Docker Compose

Codex should generate:

docker-compose.yml

Services:

api
postgres
pgadmin
keycloak

Keycloak must have its own database.

PostgreSQL Databases
dragon_envelopes
keycloak
Backend Technology Stack

Language

C#
.NET 8

ORM

EF Core

Database

PostgreSQL

Auth

Keycloak (OIDC)

Mapping

Manual mappers only
NO AutoMapper

Testing

xUnit
FluentAssertions
TestContainers

Logging

Serilog
Domain Driven Design

Project structure:

DragonEnvelopes.Domain
DragonEnvelopes.Application
DragonEnvelopes.Infrastructure
DragonEnvelopes.Api
DragonEnvelopes.Tests
Core Domain Model
Family
Family
 ├ Members
 ├ Accounts
 ├ Envelopes
 └ Budgets
Member

Represents login users.

Member
 ├ Parent
 ├ Child
 └ Viewer

Each member has:

KeycloakUserId
Email
Role
Account

Financial source:

Bank Account
Manual Account
Stripe Account (Phase 2)
Envelope

Example envelopes:

Groceries
Rent
Utilities
Kids Clothes
Entertainment
Savings
Budget
Budget
 ├ Envelope
 ├ MonthlyAmount
 └ CurrentBalance
Transaction
Transaction
 ├ Date
 ├ Merchant
 ├ Amount
 ├ Category
 └ Envelope
Phase 1 Features
Authentication

Users login through Keycloak.

Features:

Family signup
Member invitations
Role permissions
Family Management

Create family.

Invite members.

Roles:

Admin
Parent
Child
Viewer
Accounts

Users can add:

Bank accounts
Manual accounts
Cash accounts
Transaction Import

Phase 1 supports:

CSV import
Manual entry

Phase 2:

Plaid
Stripe
Envelope Budgeting

Users create envelopes:

Groceries
Rent
Utilities
Kids Clothes
Entertainment
Savings
Paycheck Allocation

Example rule:

$2000 paycheck

Rent          $800
Groceries     $400
Utilities     $200
Kids          $200
Savings       $300
Spending      $100

Funds distributed automatically.

Smart Envelope Suggestions

Analyze transaction history.

System suggests envelopes:

Example:

Netflix → Subscription envelope
Aldi → Groceries
Shell → Gas
Transaction Categorization Engine

Algorithm:

Merchant analysis
Keyword classification
User corrections
Machine learning later

Example:

Walmart purchase

Groceries: $85
Clothes: $20
Household: $10

Future retailer integrations improve accuracy.

WPF UI Codex

Goal:

Modern fintech look.

Like a React dashboard but built in WPF.

UI Technology
WPF
MVVM
CommunityToolkit.Mvvm
Visual Style
Dark theme
Minimalistic
Professional

Colors:

Background: #1E1E1E
Surface: #252526
Accent: #4CAF50
Text: #FFFFFF
Main Layout
Sidebar Navigation
Top Status Bar
Main Content Area
Sidebar
Dashboard
Accounts
Envelopes
Transactions
Budgets
Reports
Settings
Dashboard

Shows:

Total Balance
Spending This Month
Remaining Budget

Cards for envelopes:

Groceries        $180
Rent             $800
Utilities        $95
Kids Clothes     $120
Entertainment    $60
Savings          $1200
Envelope Page

Displays:

Balance
Transactions
Monthly target
Transaction Screen

Features:

Transaction list
Filters
Envelope assignment
Bulk categorization
Phase 2 – Fintech Integrations
Plaid

Purpose:

Bank account connection
Transaction sync
Balance updates

Entity:

PlaidAccount
Stripe Integration

Using **Stripe Financial Accounts and Issuing APIs.

Stripe allows platforms to create accounts that can store funds, transfer money, and issue cards linked to those balances.

Each financial account maintains its own balance separate from the platform account, enabling multiple envelope-backed accounts.

Stripe Envelope Model
Envelope
 └ Stripe Financial Account
     └ Card
Cards

Two types:

Virtual debit cards
Physical debit cards

Stripe supports issuing both and linking them to a financial account.

Example
Groceries Envelope
 └ Virtual Debit Card

Kids Clothes Envelope
 └ Virtual Debit Card

Allowance Envelope
 └ Physical Card
Spending Control

Parents control:

Card limits
Allowed merchant categories
Daily spending
Webhooks

Stripe events handled:

card_authorization
card_transaction
balance_update

System updates envelopes automatically.

Phase 2 User Experience

Example purchase:

Kid buys clothes
$60

Clothes Envelope → reduced
Parent notified
Budget updated
Long Term Features
AI Budget Coach

Suggest:

Reduce spending
Rebalance envelopes
Meal Planning

Using grocery spending:

Generate weekly meal plan
Create shopping list
Stay within grocery envelope
Retail Integrations

Potential partnerships:

Walmart
Target
Amazon

Pull item-level purchases.

Automatically split transactions.

Future Mobile Apps

Possible later:

iOS
Android

But desktop WPF remains primary.

Final System Vision

Dragon Envelopes becomes:

Budgeting platform
+ family finance system
+ automated envelopes
+ smart spending insights
+ optional fintech banking layer