// ==================================================
// Program Name   : EfUserRepository.cs
// Purpose        : EF implementation of IUserRepository
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;
using ApiApp.Domain.Interfaces;
using ApiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiApp.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public EfUserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDomain(user);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDomain(user);
    }

    public async Task<User?> GetByPhoneAsync(string phone)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        return user == null ? null : MapToDomain(user);
    }

    public async Task<User?> GetByICAsync(string icNumber)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ICNumber == icNumber);
        return user == null ? null : MapToDomain(user);
    }

    public async Task AddAsync(User user)
    {
        var efUser = MapToEf(user);
        _db.Users.Add(efUser);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        var efUser = await _db.Users.FindAsync(user.Id);
        if (efUser != null)
        {
            MapToEf(user, efUser);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _db.Users.AnyAsync(u => u.UserId == id);
    }

    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null)
    {
        return await _db.Users.AnyAsync(u => u.Email == email && (!excludeUserId.HasValue || u.UserId != excludeUserId));
    }

    public async Task<bool> IsPhoneTakenAsync(string phone, Guid? excludeUserId = null)
    {
        return await _db.Users.AnyAsync(u => u.PhoneNumber == phone && (!excludeUserId.HasValue || u.UserId != excludeUserId));
    }

    public async Task<bool> IsICTakenAsync(string ic, Guid? excludeUserId = null)
    {
        return await _db.Users.AnyAsync(u => u.ICNumber == ic && (!excludeUserId.HasValue || u.UserId != excludeUserId));
    }

    private static User MapToDomain(Models.User efUser)
    {
        return new User(
            efUser.UserId,
            efUser.UserName,
            efUser.UserPassword, // Note: This should be hash, but keeping for now
            efUser.ICNumber,
            efUser.Email,
            efUser.PhoneNumber,
            efUser.UserAge,
            efUser.RoleId
        )
        {
            Balance = efUser.Balance,
            CreatedAt = efUser.CreatedAt,
            LastUpdate = efUser.LastUpdate,
            IsDeleted = efUser.IsDeleted
        };
    }

    private static Models.User MapToEf(User domainUser, Models.User? efUser = null)
    {
        efUser ??= new Models.User();
        efUser.UserId = domainUser.Id;
        efUser.UserName = domainUser.UserName;
        efUser.UserPassword = domainUser.PasswordHash;
        efUser.ICNumber = domainUser.ICNumber;
        efUser.Email = domainUser.Email;
        efUser.PhoneNumber = domainUser.PhoneNumber;
        efUser.UserAge = domainUser.Age;
        efUser.RoleId = domainUser.RoleId;
        efUser.Balance = domainUser.Balance;
        efUser.CreatedAt = domainUser.CreatedAt;
        efUser.LastUpdate = domainUser.LastUpdate;
        efUser.IsDeleted = domainUser.IsDeleted;
        return efUser;
    }
}