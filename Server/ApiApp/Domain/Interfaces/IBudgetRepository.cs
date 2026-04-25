// ==================================================
// Program Name   : IBudgetRepository.cs
// Purpose        : Repository interface for Budget domain
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;

namespace ApiApp.Domain.Interfaces;

public interface IBudgetRepository
{
    Task<Budget?> GetByIdAsync(Guid id);
    Task<Budget?> GetByUserCategoryMonthAsync(Guid userId, string category, int year, int month);
    Task<IEnumerable<Budget>> GetByUserAsync(Guid userId);
    Task<IEnumerable<Budget>> GetByUserAndMonthAsync(Guid userId, int year, int month);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Guid id);
}

public class Budget
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Category { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal LimitAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public bool IsDeleted { get; private set; }

    private Budget() { }

    public Budget(Guid id, Guid userId, string category, int year, int month, decimal limitAmount)
    {
        Id = id;
        UserId = userId;
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Year = year;
        Month = month;
        LimitAmount = limitAmount;
        CreatedAt = DateTime.UtcNow;
        LastUpdate = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateLimit(decimal newLimit)
    {
        LimitAmount = newLimit;
        LastUpdate = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        LastUpdate = DateTime.UtcNow;
    }
}