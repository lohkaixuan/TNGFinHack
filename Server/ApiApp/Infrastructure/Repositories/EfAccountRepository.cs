// ==================================================
// Program Name   : EfAccountRepository.cs
// Purpose        : EF implementation of IAccountRepository
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.Interfaces;
using ApiApp.Domain.ValueObjects;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Infrastructure.Repositories;

public class EfAccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;

    public EfAccountRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Money?> GetBalanceAsync(Guid accountId)
    {
        // Check wallet first
        var wallet = await _db.Wallets.FindAsync(accountId);
        if (wallet != null)
            return new Money(wallet.Balance);

        // Check bank account
        var bankAccount = await _db.BankAccounts.FindAsync(accountId);
        if (bankAccount != null)
            return new Money(bankAccount.Balance);

        return null;
    }

    public async Task<bool> UpdateBalanceAsync(Guid accountId, Money newBalance)
    {
        // Check wallet first
        var wallet = await _db.Wallets.FindAsync(accountId);
        if (wallet != null)
        {
            wallet.Balance = newBalance.Amount;
            await _db.SaveChangesAsync();
            return true;
        }

        // Check bank account
        var bankAccount = await _db.BankAccounts.FindAsync(accountId);
        if (bankAccount != null)
        {
            bankAccount.Balance = newBalance.Amount;
            await _db.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> AccountExistsAsync(Guid accountId)
    {
        return await _db.Wallets.AnyAsync(w => w.WalletId == accountId) ||
               await _db.BankAccounts.AnyAsync(b => b.BankAccountId == accountId);
    }

    public async Task<string?> GetAccountTypeAsync(Guid accountId)
    {
        if (await _db.Wallets.AnyAsync(w => w.WalletId == accountId))
            return "wallet";

        if (await _db.BankAccounts.AnyAsync(b => b.BankAccountId == accountId))
            return "bank";

        return null;
    }
}