using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DragonEnvelopes.Ledger.Api.CrossCutting.OpenApi;

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    private static readonly OpenApiSecurityScheme BearerSchemeReference = new()
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [BearerSchemeReference] = []
        });

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized. Bearer token is missing, invalid, or expired."
        });

        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden. Authenticated user does not meet required role/policy."
        });
    }
}
