Create a WPF desktop application called "Envelope".

The UI must look like a modern React web application but implemented using WPF and MVVM.

Use .NET 8.

------------------------------------------------

UI DESIGN GOALS

Dark theme
Professional
Minimalistic
Do not overcrowd screens
Simple typography
Smooth transitions

Design inspiration:

Linear.app
Stripe Dashboard
Modern SaaS dashboards

------------------------------------------------

LAYOUT

Main layout should resemble a React SPA.

Window layout:

Sidebar navigation (left)
Top toolbar
Main content area

------------------------------------------------

SIDEBAR ITEMS

Dashboard
Accounts
Envelopes
Transactions
Budgets
Automation
Reports
Settings

------------------------------------------------

DASHBOARD

The only screen allowed to be information dense.

Show:

Net Worth
Envelope balances
Upcoming bills
Recent transactions
Budget health indicators

Use cards.

------------------------------------------------

ENVELOPES PAGE

List envelopes.

Columns:

Name
Balance
Budget
Last Activity

Click envelope → detail page.

------------------------------------------------

TRANSACTIONS PAGE

Table with:

Date
Merchant
Amount
Envelope
Category

Allow splitting a transaction across envelopes.

------------------------------------------------

BUDGET PAGE

Visual envelope allocations.

------------------------------------------------

ACCOUNTS PAGE

Connected banks.

------------------------------------------------

DESIGN

Use MVVM.

Folders:

Views
ViewModels
Models
Services

Use dependency injection.

------------------------------------------------

STYLE

Create a reusable theme.

Dark colors:

Background: #1E1E1E
Panel: #252526
Accent: #3A8DFF
Text: #EAEAEA

Use subtle shadows.

------------------------------------------------

COMPONENTS

Create reusable controls:

Card
MetricTile
TransactionRow
EnvelopeTile
SidebarItem

------------------------------------------------

OUTPUT

Generate:

MainWindow layout
Navigation framework
Dashboard UI
Envelopes UI
Transactions UI
MVVM structure
Dark theme styles