// ==================================================
// Program Name   : Transaction.cs
// Purpose        : Domain entity for Transaction
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string FromAccount { get; private set; }
    public string ToAccount { get; private set; }
    public Money Amount { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Item { get; private set; }
    public string? Detail { get; private set; }
    public string? Category { get; private set; }
    public string? PaymentMethod { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime LastUpdate { get; private set; }

    // Related entities IDs
    public Guid? FromUserId { get; private set; }
    public Guid? ToUserId { get; private set; }
    public Guid? FromMerchantId { get; private set; }
    public Guid? ToMerchantId { get; private set; }
    public Guid? FromBankId { get; private set; }
    public Guid? ToBankId { get; private set; }
    public Guid? FromWalletId { get; private set; }
    public Guid? ToWalletId { get; private set; }

    private Transaction() { }

    public Transaction(Guid id, string type, string fromAccount, string toAccount,
                      Money amount, DateTime timestamp, string? item = null,
                      string? detail = null, string? paymentMethod = null)
    {
        Id = id;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        FromAccount = fromAccount ?? throw new ArgumentNullException(nameof(fromAccount));
        ToAccount = toAccount ?? throw new ArgumentNullException(nameof(toAccount));
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Timestamp = timestamp;
        Item = item;
        Detail = detail;
        PaymentMethod = paymentMethod;
        Status = TransactionStatus.Pending;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetRelatedEntities(Guid? fromUserId = null, Guid? toUserId = null,
                                   Guid? fromMerchantId = null, Guid? toMerchantId = null,
                                   Guid? fromBankId = null, Guid? toBankId = null,
                                   Guid? fromWalletId = null, Guid? toWalletId = null)
    {
        FromUserId = fromUserId;
        ToUserId = toUserId;
        FromMerchantId = fromMerchantId;
        ToMerchantId = toMerchantId;
        FromBankId = fromBankId;
        ToBankId = toBankId;
        FromWalletId = fromWalletId;
        ToWalletId = toWalletId;
        LastUpdate = DateTime.UtcNow;
    }

    public void SetCategory(string category)
    {
        Category = category;
        LastUpdate = DateTime.UtcNow;
    }

    public void MarkAsSuccess()
    {
        Status = TransactionStatus.Success;
        LastUpdate = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = TransactionStatus.Failed;
        LastUpdate = DateTime.UtcNow;
    }
}

public enum TransactionStatus
{
    Pending,
    Success,
    Failed
}