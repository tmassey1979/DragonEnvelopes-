using FluentValidation;
using FluentValidation.Results;

namespace DragonEnvelopes.Ledger.Api.CrossCutting.Validation;

public static class FluentValidationEndpointFilterFactory
{
    public static EndpointFilterDelegate Create(
        EndpointFilterFactoryContext context,
        EndpointFilterDelegate next)
    {
        var validatorCandidates = context.MethodInfo
            .GetParameters()
            .Select(static (parameter, index) => new ValidatorCandidate(
                index,
                typeof(IValidator<>).MakeGenericType(parameter.ParameterType)))
            .ToArray();

        if (validatorCandidates.Length == 0)
        {
            return next;
        }

        return async invocationContext =>
        {
            var failures = new List<ValidationFailure>();

            foreach (var candidate in validatorCandidates)
            {
                var argument = invocationContext.Arguments[candidate.ArgumentIndex];
                if (argument is null)
                {
                    continue;
                }

                if (invocationContext.HttpContext.RequestServices.GetService(candidate.ValidatorType) is not IValidator validator)
                {
                    continue;
                }

                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(
                    validationContext,
                    invocationContext.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    failures.AddRange(validationResult.Errors);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            return await next(invocationContext);
        };
    }

    private sealed record ValidatorCandidate(int ArgumentIndex, Type ValidatorType);
}
