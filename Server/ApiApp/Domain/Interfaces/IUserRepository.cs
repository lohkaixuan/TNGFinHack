// ==================================================
// Program Name   : IUserRepository.cs
// Purpose        : Repository interface for User domain
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Domain.Entities;

namespace ApiApp.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phone);
    Task<User?> GetByICAsync(string icNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null);
    Task<bool> IsPhoneTakenAsync(string phone, Guid? excludeUserId = null);
    Task<bool> IsICTakenAsync(string ic, Guid? excludeUserId = null);
}