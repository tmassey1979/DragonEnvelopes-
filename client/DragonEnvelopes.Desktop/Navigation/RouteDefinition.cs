using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Navigation;

public sealed record RouteDefinition(
    string Key,
    string Label,
    string Glyph,
    string TopBarSubtitle,
    object Content,
    string? RequiredRole = null);
