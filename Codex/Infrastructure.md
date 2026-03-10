# Project: DragonEnvelopes â€“ Family Envelope Budgeting Platform

Build a production-quality project using **.NET 8**, **PostgreSQL**, **Keycloak**, and **WPF**.

The system allows families to track finances using the **envelope budgeting method**.

The architecture must follow:

* Clean Architecture
* Domain Driven Design
* SOLID principles
* Dependency Injection
* Repository Pattern
* Service Layer
* Custom mapping (NO AutoMapper)
* Test Driven Development

---

# Core Tech Stack

Backend

* ASP.NET Core 8
* Entity Framework Core
* PostgreSQL
* Keycloak authentication (OIDC)
* FluentValidation
* Serilog logging
* xUnit tests

Frontend

* WPF (.NET 8)
* MVVM
* CommunityToolkit.MVVM

Infrastructure

* Docker
* Docker Compose
* PostgreSQL
* pgAdmin4
* Keycloak

---

# System Components

1. WPF Desktop Client
2. ASP.NET Core API
3. PostgreSQL Database Server
4. Keycloak Authentication Server
5. pgAdmin4 Database UI

Everything except the WPF app must run in **Docker**.

---

# Solution Structure

DragonEnvelopes.sln

src/
DragonEnvelopes.Domain
DragonEnvelopes.Application
DragonEnvelopes.Infrastructure
DragonEnvelopes.Api
DragonEnvelopes.Contracts

client/
DragonEnvelopes.Desktop

tests/
DragonEnvelopes.Domain.Tests
DragonEnvelopes.Application.Tests

---

# Domain Layer

Contains:

Entities
Value Objects
Domain Interfaces
Domain Services

Entities:

Family
FamilyMember
Account
Envelope
Transaction
Budget

Relationships:

Family
-> many FamilyMembers
-> many Accounts
-> many Envelopes

FamilyMember
-> linked to Keycloak user

Account
-> belongs to Family

Envelope
-> belongs to Family

Transaction
-> belongs to Account
-> optionally assigned to Envelope

---

# Entity Definitions

Family

* Id (Guid)
* Name
* CreatedAt

FamilyMember

* Id (Guid)
* FamilyId
* KeycloakUserId
* Name
* Email
* Role

Account

* Id
* FamilyId
* Name
* Balance
* Type

Envelope

* Id
* FamilyId
* Name
* MonthlyBudget
* CurrentBalance

Transaction

* Id
* AccountId
* Amount
* Description
* Date
* Category
* EnvelopeId

Budget

* Id
* FamilyId
* Month
* TotalIncome

---

# Authentication

Use **OpenID Connect with Keycloak**.

Each **FamilyMember has their own login**.

Authentication Flow:

User logs in via Keycloak
Keycloak returns JWT token
WPF client sends JWT to API
API validates JWT

FamilyMember table stores:

KeycloakUserId

Mapping Keycloak user to application user.

Roles:

Parent
Adult
Teen
Child (read only)

---

# Application Layer

Contains:

Services
DTOs
Custom Mappers
Validators

Services:

FamilyService
AccountService
EnvelopeService
TransactionService
BudgetService
UserService

---

# Custom Mapping

Do NOT use AutoMapper.

Create manual mapping classes:

Example:

FamilyMapper
TransactionMapper
EnvelopeMapper

Example structure:

Application/
Mapping/
FamilyMapper.cs
EnvelopeMapper.cs
TransactionMapper.cs

Mapping must convert:

Entity -> DTO
DTO -> Entity

---

# Infrastructure Layer

Contains:

EF Core DbContext
Repository implementations
Database configuration

DbContext:

DragonEnvelopesDbContext

Tables:

families
family_members
accounts
envelopes
transactions
budgets

---

# API Layer

Expose REST endpoints.

Controllers:

AuthController
FamilyController
MembersController
AccountController
EnvelopeController
TransactionController
BudgetController

Examples:

POST /families
POST /families/{id}/members
GET /families/{id}

POST /accounts
GET /accounts

POST /envelopes
GET /envelopes

POST /transactions
GET /transactions

---

# PostgreSQL Setup

Use **two databases**:

DragonEnvelopes_app
keycloak

Keycloak uses its own database.

---

# Docker Compose Setup

Services:

postgres
pgadmin
keycloak
api

---

# PostgreSQL

Image:

postgres:16

Environment:

POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

Volumes:

postgres_data

---

# pgAdmin4

Image:

dpage/pgadmin4

Environment:

PGADMIN_DEFAULT_EMAIL=[admin@DragonEnvelopes.local](mailto:admin@DragonEnvelopes.local)
PGADMIN_DEFAULT_PASSWORD=admin

pgAdmin must automatically connect to the postgres server.

---

# Keycloak

Image:

quay.io/keycloak/keycloak

Start mode:

start-dev

Environment:

KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=admin

Database:

keycloak database in postgres.

---

# API Container

Build from local Dockerfile.

Environment:

ConnectionStrings__Default=Host=postgres;Database=DragonEnvelopes_app;Username=postgres;Password=postgres

---

# Logging

Use Serilog.

Log:

API requests
Exceptions
Authentication failures

---

# Testing

Use TDD.

Tests must exist for:

Envelope allocation
Transaction categorization
Budget calculations

Framework:

xUnit
Moq

---

# Migration Strategy

Use EF Core migrations.

Migrations run automatically when API starts.

---

# Phase 1 Feature Set

Must support:

Family creation
Family member management
Envelope budgeting
Transaction tracking
Account tracking
Budget reporting

Stripe and Plaid are NOT implemented in Phase 1.

---

# Goal

When complete the system must allow:

1. A family to register
2. Each family member to log in
3. Create envelopes
4. Track spending
5. Categorize transactions
6. See remaining budget

