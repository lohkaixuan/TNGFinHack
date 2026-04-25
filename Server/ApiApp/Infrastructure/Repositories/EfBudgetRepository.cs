// ==================================================
// Program Name   : EfBudgetRepository.cs
// Purpose        : EF implementation of IBudgetRepository
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiApp.Domain.Interfaces;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Infrastructure.Repositories;

public class EfBudgetRepository : IBudgetRepository
{
    private readonly AppDbContext _db;

    public EfBudgetRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IBudgetRepository.Budget?> GetByIdAsync(Guid id)
    {
        var budget = await _db.Budgets.FindAsync(id);
        return budget == null ? null : MapToDomain(budget);
    }

    public async Task<IBudgetRepository.Budget?> GetByUserCategoryMonthAsync(Guid userId, string category, int year, int month)
    {
        var budget = await _db.Budgets.FirstOrDefaultAsync(b =>
            b.user_id == userId &&
            b.category == category &&
            b.year == year &&
            b.month == month);

        return budget == null ? null : MapToDomain(budget);
    }

    public async Task<IEnumerable<IBudgetRepository.Budget>> GetByUserAsync(Guid userId)
    {
        var budgets = await _db.Budgets
            .Where(b => b.user_id == userId)
            .ToListAsync();

        return budgets.Select(MapToDomain);
    }

    public async Task<IEnumerable<IBudgetRepository.Budget>> GetByUserAndMonthAsync(Guid userId, int year, int month)
    {
        var budgets = await _db.Budgets
            .Where(b => b.user_id == userId && b.year == year && b.month == month)
            .ToListAsync();

        return budgets.Select(MapToDomain);
    }

    public async Task AddAsync(IBudgetRepository.Budget budget)
    {
        var efBudget = MapToEf(budget);
        _db.Budgets.Add(efBudget);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(IBudgetRepository.Budget budget)
    {
        var efBudget = await _db.Budgets.FindAsync(budget.Id);
        if (efBudget != null)
        {
            MapToEf(budget, efBudget);
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var budget = await _db.Budgets.FindAsync(id);
        if (budget != null)
        {
            budget.is_deleted = true;
            await _db.SaveChangesAsync();
        }
    }

    private static IBudgetRepository.Budget MapToDomain(Models.Budget efBudget)
    {
        return new IBudgetRepository.Budget(
            efBudget.budget_id,
            efBudget.user_id,
            efBudget.category ?? "general",
            efBudget.year,
            efBudget.month,
            efBudget.limit_amount
        )
        {
            CreatedAt = efBudget.created_at,
            LastUpdate = efBudget.last_update,
            IsDeleted = efBudget.is_deleted
        };
    }

    private static Models.Budget MapToEf(IBudgetRepository.Budget domainBudget, Models.Budget? efBudget = null)
    {
        efBudget ??= new Models.Budget();
        efBudget.budget_id = domainBudget.Id;
        efBudget.user_id = domainBudget.UserId;
        efBudget.category = domainBudget.Category;
        efBudget.year = domainBudget.Year;
        efBudget.month = domainBudget.Month;
        efBudget.limit_amount = domainBudget.LimitAmount;
        efBudget.created_at = domainBudget.CreatedAt;
        efBudget.last_update = domainBudget.LastUpdate;
        efBudget.is_deleted = domainBudget.IsDeleted;
        return efBudget;
    }
}