namespace DragonEnvelopes.Api.Endpoints;

internal static partial class AccountAndTransactionEndpoints
{
    public static RouteGroupBuilder MapAccountAndTransactionEndpoints(this RouteGroupBuilder v1)
    {
        MapAccountEndpoints(v1);
        MapTransactionEndpoints(v1);
        MapTransactionImportEndpoints(v1);

        return v1;
    }
}
