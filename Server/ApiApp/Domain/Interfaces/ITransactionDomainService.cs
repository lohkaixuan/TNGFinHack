// ==================================================
// Program Name   : ITransactionDomainService.cs
// Purpose        : Domain service interface for transaction business rules
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Domain.Interfaces;

public interface ITransactionDomainService
{
    Task<bool> CanTransferAsync(Guid fromAccountId, Guid toAccountId, Money amount);
    Task ExecuteTransferAsync(Guid fromAccountId, Guid toAccountId, Money amount, string type);
}