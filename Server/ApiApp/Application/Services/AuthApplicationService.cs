// ==================================================
// Program Name   : AuthApplicationService.cs
// Purpose        : Application service for authentication operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using System.Threading.Tasks;
using ApiApp.Application.Commands;
using ApiApp.Domain.Entities;
using ApiApp.Domain.Interfaces;

namespace ApiApp.Application.Services;

public class AuthApplicationService
{
    private readonly IUserRepository _userRepository;

    public AuthApplicationService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> RegisterUserAsync(RegisterUserCommand command)
    {
        // Validate uniqueness
        if (await _userRepository.IsEmailTakenAsync(command.Email))
            throw new ArgumentException("Email already taken");

        if (await _userRepository.IsPhoneTakenAsync(command.PhoneNumber))
            throw new ArgumentException("Phone number already taken");

        if (await _userRepository.IsICTakenAsync(command.ICNumber))
            throw new ArgumentException("IC number already taken");

        // For now, using plain password - in production, hash it
        var user = new User(
            Guid.NewGuid(),
            command.UserName,
            command.Password, // TODO: Hash password
            command.ICNumber,
            command.Email,
            command.PhoneNumber,
            command.Age,
            Guid.Parse("11111111-1111-1111-1111-111111111001") // ROLE_USER
        );

        await _userRepository.AddAsync(user);
        return user;
    }

    public async Task<User?> AuthenticateAsync(string identifier, string password)
    {
        User? user = null;

        if (identifier.Contains("@"))
            user = await _userRepository.GetByEmailAsync(identifier);
        else
            user = await _userRepository.GetByPhoneAsync(identifier);

        if (user == null || user.PasswordHash != password) // TODO: Verify hash
            return null;

        return user;
    }
}