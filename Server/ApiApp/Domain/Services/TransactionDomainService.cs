// ==================================================
// Program Name   : TransactionDomainService.cs
// Purpose        : Domain service for transaction business rules
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;
using ApiApp.Domain.Interfaces;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Domain.Services;

public class TransactionDomainService : ITransactionDomainService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TransactionDomainService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> CanTransferAsync(Guid fromAccountId, Guid toAccountId, Money amount)
    {
        if (fromAccountId == toAccountId)
            return false;

        var fromBalance = await _accountRepository.GetBalanceAsync(fromAccountId);
        if (fromBalance == null)
            return false;

        return fromBalance.IsGreaterThan(amount) || fromBalance.Amount == amount.Amount;
    }

    public async Task ExecuteTransferAsync(Guid fromAccountId, Guid toAccountId, Money amount)
    {
        if (!await CanTransferAsync(fromAccountId, toAccountId, amount))
            throw new InvalidOperationException("Transfer not allowed");

        var fromBalance = await _accountRepository.GetBalanceAsync(fromAccountId);
        var toBalance = await _accountRepository.GetBalanceAsync(toAccountId);

        if (fromBalance == null || toBalance == null)
            throw new InvalidOperationException("Account not found");

        var newFromBalance = fromBalance.Subtract(amount);
        var newToBalance = toBalance.Add(amount);

        await _accountRepository.UpdateBalanceAsync(fromAccountId, newFromBalance);
        await _accountRepository.UpdateBalanceAsync(toAccountId, newToBalance);

        // Transaction record creation is handled by application service
    }
}