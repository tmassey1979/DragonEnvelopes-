namespace DragonEnvelopes.Api.CrossCutting.Validation;

public static class ValidationEndpointExtensions
{
    public static RouteHandlerBuilder AddFluentValidation(this RouteHandlerBuilder builder)
    {
        builder.AddEndpointFilterFactory(FluentValidationEndpointFilterFactory.Create);
        return builder;
    }

    public static RouteGroupBuilder AddFluentValidation(this RouteGroupBuilder builder)
    {
        builder.AddEndpointFilterFactory(FluentValidationEndpointFilterFactory.Create);
        return builder;
    }
}
