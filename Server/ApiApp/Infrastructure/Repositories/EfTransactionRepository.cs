// ==================================================
// Program Name   : EfTransactionRepository.cs
// Purpose        : EF implementation of ITransactionRepository
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;
using ApiApp.Domain.Interfaces;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Infrastructure.Repositories;

public class EfTransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public EfTransactionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        return transaction == null ? null : MapToDomain(transaction);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Transactions
            .Where(t => t.from_user_id == userId || t.to_user_id == userId);

        if (from.HasValue)
            query = query.Where(t => t.transaction_timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.transaction_timestamp <= to.Value);

        var transactions = await query.ToListAsync();
        return transactions.Select(MapToDomain);
    }

    public async Task<IEnumerable<Transaction>> GetByMerchantIdAsync(Guid merchantId, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Transactions
            .Where(t => t.from_merchant_id == merchantId || t.to_merchant_id == merchantId);

        if (from.HasValue)
            query = query.Where(t => t.transaction_timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.transaction_timestamp <= to.Value);

        var transactions = await query.ToListAsync();
        return transactions.Select(MapToDomain);
    }

    public async Task AddAsync(Transaction transaction)
    {
        var efTransaction = MapToEf(transaction);
        _db.Transactions.Add(efTransaction);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        var efTransaction = await _db.Transactions.FindAsync(transaction.Id);
        if (efTransaction != null)
        {
            MapToEf(transaction, efTransaction);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Transaction>> GetPendingTransactionsAsync()
    {
        var transactions = await _db.Transactions
            .Where(t => t.transaction_status == "pending")
            .ToListAsync();

        return transactions.Select(MapToDomain);
    }

    private static Transaction MapToDomain(Models.Transaction efTransaction)
    {
        var transaction = new Transaction(
            efTransaction.transaction_id,
            efTransaction.transaction_type,
            efTransaction.transaction_from,
            efTransaction.transaction_to,
            new Domain.ValueObjects.Money(efTransaction.transaction_amount),
            efTransaction.transaction_timestamp,
            efTransaction.transaction_item,
            efTransaction.transaction_detail,
            efTransaction.payment_method
        );

        transaction.SetRelatedEntities(
            efTransaction.from_user_id,
            efTransaction.to_user_id,
            efTransaction.from_merchant_id,
            efTransaction.to_merchant_id,
            efTransaction.from_bank_id,
            efTransaction.to_bank_id,
            efTransaction.from_wallet_id,
            efTransaction.to_wallet_id
        );

        if (!string.IsNullOrEmpty(efTransaction.category))
            transaction.SetCategory(efTransaction.category);

        if (efTransaction.transaction_status == "success")
            transaction.MarkAsSuccess();
        else if (efTransaction.transaction_status == "failed")
            transaction.MarkAsFailed();

        return transaction;
    }

    private static Models.Transaction MapToEf(Transaction domainTransaction, Models.Transaction? efTransaction = null)
    {
        efTransaction ??= new Models.Transaction();
        efTransaction.transaction_id = domainTransaction.Id;
        efTransaction.transaction_type = domainTransaction.Type;
        efTransaction.transaction_from = domainTransaction.FromAccount;
        efTransaction.transaction_to = domainTransaction.ToAccount;
        efTransaction.transaction_amount = domainTransaction.Amount.Amount;
        efTransaction.transaction_timestamp = domainTransaction.Timestamp;
        efTransaction.transaction_item = domainTransaction.Item;
        efTransaction.transaction_detail = domainTransaction.Detail;
        efTransaction.category = domainTransaction.Category;
        efTransaction.payment_method = domainTransaction.PaymentMethod;
        efTransaction.transaction_status = domainTransaction.Status.ToString().ToLower();
        efTransaction.last_update = domainTransaction.LastUpdate;

        efTransaction.from_user_id = domainTransaction.FromUserId;
        efTransaction.to_user_id = domainTransaction.ToUserId;
        efTransaction.from_merchant_id = domainTransaction.FromMerchantId;
        efTransaction.to_merchant_id = domainTransaction.ToMerchantId;
        efTransaction.from_bank_id = domainTransaction.FromBankId;
        efTransaction.to_bank_id = domainTransaction.ToBankId;
        efTransaction.from_wallet_id = domainTransaction.FromWalletId;
        efTransaction.to_wallet_id = domainTransaction.ToWalletId;

        return efTransaction;
    }
}