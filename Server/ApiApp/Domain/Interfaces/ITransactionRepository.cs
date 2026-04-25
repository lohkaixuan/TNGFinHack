// ==================================================
// Program Name   : ITransactionRepository.cs
// Purpose        : Repository interface for Transaction domain
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;

namespace ApiApp.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<Transaction>> GetByMerchantIdAsync(Guid merchantId, DateTime? from = null, DateTime? to = null);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<IEnumerable<Transaction>> GetPendingTransactionsAsync();
}