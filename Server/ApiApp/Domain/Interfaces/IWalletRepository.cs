// ==================================================
// Program Name   : IWalletRepository.cs
// Purpose        : Repository interface for Wallet domain
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Domain.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<Wallet?> GetByUserAsync(Guid userId);
    Task<Wallet?> GetByMerchantAsync(Guid merchantId);
    Task<IEnumerable<Wallet>> GetAllByUserAsync(Guid userId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? MerchantId { get; private set; }
    public Money Balance { get; private set; }
    public string WalletNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public bool IsDeleted { get; private set; }

    private Wallet() { }

    public Wallet(Guid id, Guid? userId, Guid? merchantId, string walletNumber = "")
    {
        Id = id;
        UserId = userId;
        MerchantId = merchantId;
        WalletNumber = walletNumber;
        Balance = new Money(0);
        CreatedAt = DateTime.UtcNow;
        LastUpdate = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void Deposit(Money amount)
    {
        Balance = Balance.Add(amount);
        LastUpdate = DateTime.UtcNow;
    }

    public void Withdraw(Money amount)
    {
        Balance = Balance.Subtract(amount);
        LastUpdate = DateTime.UtcNow;
    }

    public bool CanWithdraw(Money amount)
    {
        return Balance.IsGreaterThan(amount) || Balance.Amount == amount.Amount;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        LastUpdate = DateTime.UtcNow;
    }
}