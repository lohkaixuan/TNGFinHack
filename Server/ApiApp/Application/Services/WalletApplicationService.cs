// ==================================================
// Program Name   : WalletApplicationService.cs
// Purpose        : Application service for wallet operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiApp.Domain.Interfaces;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Application.Services;

public class WalletApplicationService
{
    private readonly IWalletRepository _walletRepository;

    public WalletApplicationService(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<IWalletRepository.Wallet> CreateWalletAsync(Guid? userId, Guid? merchantId)
    {
        if (userId == null && merchantId == null)
            throw new ArgumentException("Must provide either userId or merchantId");

        if (userId != null && merchantId != null)
            throw new ArgumentException("Cannot create wallet for both user and merchant");

        var existing = userId.HasValue
            ? await _walletRepository.GetByUserAsync(userId.Value)
            : await _walletRepository.GetByMerchantAsync(merchantId!.Value);

        if (existing != null)
            return existing;

        var wallet = new IWalletRepository.Wallet(
            Guid.NewGuid(),
            userId,
            merchantId,
            Guid.NewGuid().ToString("N").Substring(0, 12)
        );

        await _walletRepository.AddAsync(wallet);
        return wallet;
    }

    public async Task<IWalletRepository.Wallet?> GetWalletAsync(Guid walletId)
    {
        return await _walletRepository.GetByIdAsync(walletId);
    }

    public async Task<IWalletRepository.Wallet?> GetUserWalletAsync(Guid userId)
    {
        return await _walletRepository.GetByUserAsync(userId);
    }

    public async Task<IEnumerable<IWalletRepository.Wallet>> GetUserWalletsAsync(Guid userId)
    {
        return await _walletRepository.GetAllByUserAsync(userId);
    }

    public async Task DepositAsync(Guid walletId, Money amount)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId);
        if (wallet == null)
            throw new ArgumentException("Wallet not found");

        wallet.Deposit(amount);
        await _walletRepository.UpdateAsync(wallet);
    }

    public async Task WithdrawAsync(Guid walletId, Money amount)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId);
        if (wallet == null)
            throw new ArgumentException("Wallet not found");

        if (!wallet.CanWithdraw(amount))
            throw new InvalidOperationException("Insufficient balance");

        wallet.Withdraw(amount);
        await _walletRepository.UpdateAsync(wallet);
    }

    public async Task<Money> GetBalanceAsync(Guid walletId)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId);
        if (wallet == null)
            throw new ArgumentException("Wallet not found");

        return wallet.Balance;
    }
}