using System.Net;
using FluentValidation;
using Oficina.Estoque.Application.Shared;

namespace Oficina.Estoque.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                ?? context.TraceIdentifier;

            if (ex is EstoqueException estoqueException)
            {
                logger.LogWarning(ex, "Handled estoque exception. CorrelationId: {CorrelationId}", correlationId);
                context.Response.StatusCode = estoqueException.StatusCode;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = $"https://httpstatuses.com/{estoqueException.StatusCode}",
                    title = estoqueException.Message,
                    status = estoqueException.StatusCode,
                    code = estoqueException.Code,
                    correlationId
                });
                return;
            }

            if (ex is ValidationException validationException)
            {
                logger.LogWarning(ex, "Validation exception. CorrelationId: {CorrelationId}", correlationId);
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/400",
                    title = "Validation Error",
                    status = StatusCodes.Status400BadRequest,
                    errors = validationException.Errors.Select(error => new { error.PropertyName, error.ErrorMessage }),
                    correlationId
                });
                return;
            }

            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = StatusCodes.Status500InternalServerError,
                correlationId
            });
        }
    }
}
