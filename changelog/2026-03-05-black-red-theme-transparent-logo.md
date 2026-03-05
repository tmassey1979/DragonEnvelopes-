# 2026-03-05 - black red branding and transparent logo

## Summary
- Updated desktop dark theme palette to black/red brand colors.
- Added transparent logo asset and packaged it as a WPF resource.
- Applied logo usage in sidebar shell header and native login window.

## Files Changed
- client/DragonEnvelopes.Desktop/Resources/Themes/Colors.Dark.xaml
- client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj
- client/DragonEnvelopes.Desktop/MainWindow.xaml
- client/DragonEnvelopes.Desktop/Views/LoginWindow.xaml
- client/DragonEnvelopes.Desktop/Assets/dragonenvelopes-logo-transparent.png

## Validation
- `dotnet build client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release`
