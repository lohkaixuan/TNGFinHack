// ==================================================
// Program Name   : User.cs
// Purpose        : Domain entity for User
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;

namespace ApiApp.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string UserName { get; private set; }
    public int? Age { get; private set; }
    public Guid RoleId { get; private set; }
    public string PasswordHash { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string ICNumber { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdate { get; private set; }
    public bool IsDeleted { get; private set; }

    private User() { } // EF constructor

    public User(Guid id, string userName, string passwordHash, string icNumber,
                string? email, string? phoneNumber, int? age, Guid roleId)
    {
        Id = id;
        UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        ICNumber = icNumber ?? throw new ArgumentNullException(nameof(icNumber));
        Email = email;
        PhoneNumber = phoneNumber;
        Age = age;
        RoleId = roleId;
        Balance = 0;
        CreatedAt = DateTime.UtcNow;
        LastUpdate = DateTime.UtcNow;
        IsDeleted = false;
    }

    public void UpdateContactInfo(string? email, string? phoneNumber)
    {
        Email = email;
        PhoneNumber = phoneNumber;
        LastUpdate = DateTime.UtcNow;
    }

    public void UpdateBalance(decimal newBalance)
    {
        if (newBalance < 0)
            throw new InvalidOperationException("Balance cannot be negative");

        Balance = newBalance;
        LastUpdate = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        LastUpdate = DateTime.UtcNow;
    }
}