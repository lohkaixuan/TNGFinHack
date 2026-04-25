// ==================================================
// Program Name   : TransactionApplicationService.cs
// Purpose        : Application service for transaction operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Application.Commands;
using ApiApp.Domain.Entities;
using ApiApp.Domain.Interfaces;

namespace ApiApp.Application.Services;

public class TransactionApplicationService
{
    private readonly ITransactionDomainService _transactionDomainService;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TransactionApplicationService(
        ITransactionDomainService transactionDomainService,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository)
    {
        _transactionDomainService = transactionDomainService;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionCommand command)
    {
        // Validate accounts exist
        if (!await _accountRepository.AccountExistsAsync(command.FromAccountId))
            throw new ArgumentException("From account does not exist");

        if (!await _accountRepository.AccountExistsAsync(command.ToAccountId))
            throw new ArgumentException("To account does not exist");

        // Execute the transfer
        await _transactionDomainService.ExecuteTransferAsync(
            command.FromAccountId,
            command.ToAccountId,
            command.Amount);

        // Get the created transaction (assuming the domain service creates it)
        // For simplicity, we'll create it here, but ideally domain service should return it
        var transaction = new Transaction(
            Guid.NewGuid(),
            command.Type,
            command.FromAccountId.ToString(),
            command.ToAccountId.ToString(),
            command.Amount,
            DateTime.UtcNow,
            command.Item,
            command.Detail,
            command.PaymentMethod
        );

        // Set related entities if user provided
        if (command.UserId.HasValue)
        {
            // Determine if from/to are wallets or banks
            var fromType = await _accountRepository.GetAccountTypeAsync(command.FromAccountId);
            var toType = await _accountRepository.GetAccountTypeAsync(command.ToAccountId);

            if (fromType == "wallet")
                transaction.SetRelatedEntities(fromWalletId: command.FromAccountId, toWalletId: command.ToAccountId);
            else if (fromType == "bank")
                transaction.SetRelatedEntities(fromBankId: command.FromAccountId, toBankId: command.ToAccountId);
        }

        await _transactionRepository.AddAsync(transaction);
        transaction.MarkAsSuccess();

        return transaction;
    }

    public async Task UpdateTransactionAsync(Domain.Entities.Transaction transaction)
    {
        await _transactionRepository.UpdateAsync(transaction);
    }
}