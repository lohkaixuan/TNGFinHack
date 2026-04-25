// ==================================================
// Program Name   : RegisterUserCommand.cs
// Purpose        : Command for user registration
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;

namespace ApiApp.Application.Commands;

public class RegisterUserCommand
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ICNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? Age { get; set; }

    public RegisterUserCommand() { }

    public RegisterUserCommand(string userName, string password, string icNumber,
                              string? email = null, string? phoneNumber = null, int? age = null)
    {
        UserName = userName;
        Password = password;
        ICNumber = icNumber;
        Email = email;
        PhoneNumber = phoneNumber;
        Age = age;
    }
}