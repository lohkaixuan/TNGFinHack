// ==================================================
// Program Name   : EfWalletRepository.cs
// Purpose        : EF implementation of IWalletRepository
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiApp.Domain.Interfaces;
using ApiApp.Domain.ValueObjects;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Infrastructure.Repositories;

public class EfWalletRepository : IWalletRepository
{
    private readonly AppDbContext _db;

    public EfWalletRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IWalletRepository.Wallet?> GetByIdAsync(Guid id)
    {
        var wallet = await _db.Wallets.FindAsync(id);
        return wallet == null ? null : MapToDomain(wallet);
    }

    public async Task<IWalletRepository.Wallet?> GetByUserAsync(Guid userId)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.user_id == userId);
        return wallet == null ? null : MapToDomain(wallet);
    }

    public async Task<IWalletRepository.Wallet?> GetByMerchantAsync(Guid merchantId)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.merchant_id == merchantId);
        return wallet == null ? null : MapToDomain(wallet);
    }

    public async Task<IEnumerable<IWalletRepository.Wallet>> GetAllByUserAsync(Guid userId)
    {
        var wallets = await _db.Wallets.Where(w => w.user_id == userId).ToListAsync();
        return wallets.Select(MapToDomain);
    }

    public async Task AddAsync(IWalletRepository.Wallet wallet)
    {
        var efWallet = MapToEf(wallet);
        _db.Wallets.Add(efWallet);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(IWalletRepository.Wallet wallet)
    {
        var efWallet = await _db.Wallets.FindAsync(wallet.Id);
        if (efWallet != null)
        {
            MapToEf(wallet, efWallet);
            await _db.SaveChangesAsync();
        }
    }

    private static IWalletRepository.Wallet MapToDomain(Models.Wallet efWallet)
    {
        return new IWalletRepository.Wallet(
            efWallet.wallet_id,
            efWallet.user_id,
            efWallet.merchant_id,
            efWallet.wallet_number ?? Guid.NewGuid().ToString("N").Substring(0, 12)
        )
        {
            Balance = new Money(efWallet.wallet_balance),
            CreatedAt = efWallet.created_at ?? DateTime.UtcNow,
            LastUpdate = efWallet.last_update,
            IsDeleted = efWallet.is_deleted ?? false
        };
    }

    private static Models.Wallet MapToEf(IWalletRepository.Wallet domainWallet, Models.Wallet? efWallet = null)
    {
        efWallet ??= new Models.Wallet();
        efWallet.wallet_id = domainWallet.Id;
        efWallet.user_id = domainWallet.UserId;
        efWallet.merchant_id = domainWallet.MerchantId;
        efWallet.wallet_balance = domainWallet.Balance.Amount;
        efWallet.wallet_number = domainWallet.WalletNumber;
        efWallet.last_update = domainWallet.LastUpdate;
        efWallet.is_deleted = domainWallet.IsDeleted;
        return efWallet;
    }
}