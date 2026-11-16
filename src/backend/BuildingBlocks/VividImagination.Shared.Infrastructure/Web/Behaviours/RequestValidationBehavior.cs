using FluentValidation;
using MediatR;
using ValidationException = VividImagination.Shared.Infrastructure.Exceptions.ValidationException;

namespace VividImagination.Shared.Infrastructure.Web.Behaviours
{
    public class RequestValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var context = new ValidationContext<TRequest>(request);

            var validationTasks = validators.Select(v => v.ValidateAsync(context, cancellationToken));
            var results = await Task.WhenAll(validationTasks);

            var failures = results
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }

            return await next(cancellationToken);
            
        }
    }
}