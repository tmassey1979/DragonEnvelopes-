namespace DragonEnvelopes.Desktop.Auth;

public sealed record AuthSignInResult(
    bool Succeeded,
    bool Cancelled,
    string Message,
    AuthSession? Session = null);
