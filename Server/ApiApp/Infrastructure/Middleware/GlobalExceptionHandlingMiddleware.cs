// ==================================================
// Program Name   : GlobalExceptionHandlingMiddleware.cs
// Purpose        : Middleware for global exception handling
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ApiApp.Domain.Exceptions;

namespace ApiApp.Infrastructure.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.ErrorCode = ex.ErrorCode;
                break;

            case EntityNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = ex.Message;
                response.ErrorCode = ex.ErrorCode;
                break;

            case InsufficientFundsException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.ErrorCode = ex.ErrorCode;
                break;

            case InvalidOperationException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.ErrorCode = ex.ErrorCode;
                break;

            case DomainException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.ErrorCode = ex.ErrorCode;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                response.ErrorCode = "INTERNAL_ERROR";
                break;
        }

        context.Response.StatusCode = response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}