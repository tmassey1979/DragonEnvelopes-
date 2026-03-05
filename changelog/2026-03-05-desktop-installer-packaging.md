# 2026-03-05 - desktop installer packaging

## Summary
- Added a WiX Toolset v4 installer project for the WPF desktop app.
- Wired CI to publish the desktop payload, build an MSI, and upload installer artifacts.
- Documented local commands to produce the MSI installer package.

## Files Changed
- .github/workflows/ci.yml
- README.md
- installer/DragonEnvelopes.Desktop.Installer/DragonEnvelopes.Desktop.Installer.wixproj
- installer/DragonEnvelopes.Desktop.Installer/Package.wxs

## Validation
- `dotnet publish client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:SatelliteResourceLanguages=en --output artifacts/desktop/publish`
- `dotnet build installer/DragonEnvelopes.Desktop.Installer/DragonEnvelopes.Desktop.Installer.wixproj --configuration Release -p:ProductVersion=1.0.0 -p:PublishDir="c:\code\Playground\DragonEnvelopes\artifacts\desktop\publish"`
