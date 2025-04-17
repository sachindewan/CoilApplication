using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Coil.Api.Shared.Exception.Handler
{
    public class GlobalExceptionMiddleware : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
        {
            _logger = logger;
        }
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, System.Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred.");

            var correlationId = Guid.NewGuid().ToString();
            httpContext.Response.ContentType = "application/problem+json";

            if (exception is ValidationException validationException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var validationProblemDetails = new ValidationProblemDetails
                {
                    Status = httpContext.Response.StatusCode,
                    Title = "Validation error occurred.",
                    Detail = "One or more validation errors occurred.",
                    Instance = httpContext.Request.Path,
                    Extensions = { ["correlationId"] = correlationId }
                };

                foreach (var error in validationException.Errors)
                {
                    validationProblemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
                }

                var json = JsonSerializer.Serialize(validationProblemDetails);
                await httpContext.Response.WriteAsync(json, cancellationToken);
            }
            else
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var problemDetails = new ProblemDetails
                {
                    Status = httpContext.Response.StatusCode,
                    Title = "An unexpected error occurred.",
                    Detail = exception.Message,
                    Instance = httpContext.Request.Path,
                    Extensions = { ["correlationId"] = correlationId }
                };

                var json = JsonSerializer.Serialize(problemDetails);
                await httpContext.Response.WriteAsync(json, cancellationToken);
            }

            return true;
        }
    }
}
