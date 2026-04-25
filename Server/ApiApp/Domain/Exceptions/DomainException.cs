// ==================================================
// Program Name   : DomainException.cs
// Purpose        : Base domain exception for business rule violations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;

namespace ApiApp.Domain.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message, string errorCode = "DOMAIN_ERROR") : base(message)
    {
        ErrorCode = errorCode;
    }
}

public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message, "VALIDATION_ERROR") { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with ID '{id}' not found", "ENTITY_NOT_FOUND") { }
}

public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException()
        : base("Insufficient funds for this transaction", "INSUFFICIENT_FUNDS") { }
}

public class InvalidOperationException : DomainException
{
    public InvalidOperationException(string message)
        : base(message, "INVALID_OPERATION") { }
}