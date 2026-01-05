using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using System.Net;

namespace Auth.Api.Filters;

public class GlobalExceptionFilter(
    ILogger<GlobalExceptionFilter> logger,
    IHostEnvironment environment) : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        _logger.LogError(
            exception,
            "Erro não tratado: {Message} | Path: {Path}",
            exception.Message,
            context.HttpContext.Request.Path);

        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            UnauthorizedAccessException => HandleUnauthorizedException(exception),
            KeyNotFoundException => HandleNotFoundException(exception),
            InvalidOperationException => HandleInvalidOperationException(exception),
            ArgumentException => HandleArgumentException(exception),
            _ => HandleGenericException(exception)
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }

    private (int StatusCode, object Response) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return (
            (int)HttpStatusCode.BadRequest,
            new
            {
                type = "ValidationError",
                title = "Um ou mais erros de validação ocorreram",
                status = 400,
                errors
            }
        );
    }

    private (int StatusCode, object Response) HandleUnauthorizedException(Exception exception)
    {
        return (
            (int)HttpStatusCode.Unauthorized,
            new
            {
                type = "UnauthorizedError",
                title = "Não autorizado",
                status = 401,
                message = exception.Message
            }
        );
    }

    private (int StatusCode, object Response) HandleNotFoundException(Exception exception)
    {
        return (
            (int)HttpStatusCode.NotFound,
            new
            {
                type = "NotFoundError",
                title = "Recurso não encontrado",
                status = 404,
                message = exception.Message
            }
        );
    }

    private (int StatusCode, object Response) HandleInvalidOperationException(Exception exception)
    {
        return (
            (int)HttpStatusCode.BadRequest,
            new
            {
                type = "InvalidOperationError",
                title = "Operação inválida",
                status = 400,
                message = exception.Message
            }
        );
    }

    private (int StatusCode, object Response) HandleArgumentException(Exception exception)
    {
        return (
            (int)HttpStatusCode.BadRequest,
            new
            {
                type = "ArgumentError",
                title = "Argumento inválido",
                status = 400,
                message = exception.Message
            }
        );
    }

    private (int StatusCode, object Response) HandleGenericException(Exception exception)
    {
        var message = _environment.IsDevelopment()
            ? exception.Message
            : "Ocorreu um erro interno no servidor";

        var response = new
        {
            type = "InternalServerError",
            title = "Erro interno do servidor",
            status = 500,
            message
        };

        if (_environment.IsDevelopment())
        {
            return (
                (int)HttpStatusCode.InternalServerError,
                new
                {
                    response.type,
                    response.title,
                    response.status,
                    response.message,
                    stackTrace = exception.StackTrace,
                    innerException = exception.InnerException?.Message
                }
            );
        }

        return ((int)HttpStatusCode.InternalServerError, response);
    }
}