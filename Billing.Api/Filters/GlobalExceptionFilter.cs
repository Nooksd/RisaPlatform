using Billing.Domain.DTOs.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Billing.Api.Filters;

public sealed class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment) : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public void OnException(ExceptionContext context)
    {
        _logger.LogError(context.Exception, "Unhandled exception occurred");

        var (statusCode, response) = context.Exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(
                    "Erro de validação",
                    "VALIDATION_ERROR",
                    validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()))),

            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(argEx.Message, "INVALID_ARGUMENT", null)),

            KeyNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ApiErrorResponse(notFoundEx.Message, "NOT_FOUND", null)),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ApiErrorResponse("Não autorizado", "UNAUTHORIZED", null)),

            InvalidOperationException invalidOpEx => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(invalidOpEx.Message, "INVALID_OPERATION", null)),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    _environment.IsDevelopment()
                        ? context.Exception.Message
                        : "Erro interno do servidor",
                    "INTERNAL_ERROR",
                    null))
        };

        context.Result = new ObjectResult(response) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
