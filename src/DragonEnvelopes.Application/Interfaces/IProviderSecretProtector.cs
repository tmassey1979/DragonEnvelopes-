namespace DragonEnvelopes.Application.Interfaces;

public interface IProviderSecretProtector
{
    string Protect(string value);

    string Unprotect(string value);

    bool IsProtected(string value);
}
