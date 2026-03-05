# 2026-03-05 - Reusable TransactionRow Control

## Summary

Implemented a reusable `TransactionRowControl` with interaction states and action command bindings, then integrated it into a virtualized transactions list in shell templates.

## Completed Story

- #58 Build reusable TransactionRow control with interaction states

## Key Changes

- Added transaction row view model:
  - `client/DragonEnvelopes.Desktop/ViewModels/TransactionRowViewModel.cs`
- Added reusable transaction row control:
  - Fields: date, merchant, amount, envelope, category
  - States: selected, edited, flagged, hover, keyboard focus
  - Commands: edit, split (dependency properties)
  - `client/DragonEnvelopes.Desktop/Controls/TransactionRowControl.xaml`
  - `client/DragonEnvelopes.Desktop/Controls/TransactionRowControl.xaml.cs`
- Extended shell content model:
  - Added `Transactions` collection to `ShellContentViewModel`
  - `client/DragonEnvelopes.Desktop/ViewModels/ShellContentViewModel.cs`
- Added sample transaction rows to route registry:
  - Includes selected/edited/flagged scenarios and no-op edit/split commands
  - `client/DragonEnvelopes.Desktop/Navigation/RouteRegistry.cs`
- Integrated control into shell template:
  - Added virtualized `ListBox` rendering `TransactionRowControl` items
  - `client/DragonEnvelopes.Desktop/Resources/Templates/ShellTemplates.xaml`

## Validation

- `dotnet build DragonEnvelopes.sln -c Release`
- `dotnet test DragonEnvelopes.sln -c Release --no-build`
