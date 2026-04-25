// ==================================================
// Program Name   : IAccountRepository.cs
// Purpose        : Repository interface for accounts (wallets, bank accounts)
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Money?> GetBalanceAsync(Guid accountId);
    Task<bool> UpdateBalanceAsync(Guid accountId, Money newBalance);
    Task<bool> AccountExistsAsync(Guid accountId);
    Task<string?> GetAccountTypeAsync(Guid accountId);
}