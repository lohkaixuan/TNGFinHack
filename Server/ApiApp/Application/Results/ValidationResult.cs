// ==================================================
// Program Name   : ValidationResult.cs
// Purpose        : Result object for validation operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiApp.Application.Results;

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public List<ValidationError> Errors { get; private set; }

    private ValidationResult()
    {
        Errors = new List<ValidationError>();
        IsValid = true;
    }

    public static ValidationResult Success() => new();

    public static ValidationResult Failure(string message, string field = "")
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<ValidationError> { new ValidationError(field, message) }
        };
    }

    public static ValidationResult Failure(List<ValidationError> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    public void AddError(string message, string field = "")
    {
        IsValid = false;
        Errors.Add(new ValidationError(field, message));
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            var errorMessages = string.Join("; ", Errors.Select(e => e.Message));
            throw new Exceptions.ValidationException(errorMessages);
        }
    }
}

public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }

    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}