// ==================================================
// Program Name   : BudgetApplicationService.cs
// Purpose        : Application service for budget operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiApp.Domain.Interfaces;

namespace ApiApp.Application.Services;

public class BudgetApplicationService
{
    private readonly IBudgetRepository _budgetRepository;

    public BudgetApplicationService(IBudgetRepository budgetRepository)
    {
        _budgetRepository = budgetRepository;
    }

    public async Task<IBudgetRepository.Budget> UpsertBudgetAsync(Guid userId, string category, int year, int month, decimal limitAmount)
    {
        if (limitAmount < 0)
            throw new ArgumentException("Limit amount cannot be negative");

        var existing = await _budgetRepository.GetByUserCategoryMonthAsync(userId, category, year, month);

        if (existing != null)
        {
            existing.UpdateLimit(limitAmount);
            await _budgetRepository.UpdateAsync(existing);
            return existing;
        }

        var budget = new IBudgetRepository.Budget(
            Guid.NewGuid(),
            userId,
            category,
            year,
            month,
            limitAmount
        );

        await _budgetRepository.AddAsync(budget);
        return budget;
    }

    public async Task<IBudgetRepository.Budget?> GetBudgetAsync(Guid budgetId)
    {
        return await _budgetRepository.GetByIdAsync(budgetId);
    }

    public async Task<IEnumerable<IBudgetRepository.Budget>> GetUserBudgetsAsync(Guid userId)
    {
        return await _budgetRepository.GetByUserAsync(userId);
    }

    public async Task<IEnumerable<IBudgetRepository.Budget>> GetMonthlyBudgetsAsync(Guid userId, int year, int month)
    {
        return await _budgetRepository.GetByUserAndMonthAsync(userId, year, month);
    }

    public async Task DeleteBudgetAsync(Guid budgetId)
    {
        var budget = await _budgetRepository.GetByIdAsync(budgetId);
        if (budget != null)
        {
            budget.MarkAsDeleted();
            await _budgetRepository.UpdateAsync(budget);
        }
    }
}