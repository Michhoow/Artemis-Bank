using ArtemisBank.Core.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Behaviors
{
    /// <summary>
    /// Behavior transversal: ejecuta los validadores de FluentValidation de cada Command/Query
    /// ANTES de llegar al handler.
    ///
    /// Separacion deliberada:
    ///  - Aqui viven las validaciones ESTRUCTURALES (requeridos, rangos, formatos, enums permitidos).
    ///  - Las reglas de negocio con acceso a datos (fondos, propiedad de la cuenta, estado del
    ///    producto) viven en los servicios de negocio, no en los validadores.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any()) return await next();

            var context = new ValidationContext<TRequest>(request);

            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = results
                .Where(r => !r.IsValid)
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Count > 0)
            {
                var errors = failures
                    .GroupBy(f => f.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

                throw new AppValidationException(errors);
            }

            return await next();
        }
    }
}
