// ==================================================
// Program Name   : CreateTransactionCommand.cs
// Purpose        : Command for creating transactions
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;
using ApiApp.Domain.ValueObjects;

namespace ApiApp.Application.Commands;

public class CreateTransactionCommand
{
    public string Type { get; set; } = "pay";
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public Money Amount { get; set; } = null!;
    public string? Item { get; set; }
    public string? Detail { get; set; }
    public string? PaymentMethod { get; set; }
    public Guid? UserId { get; set; }

    public CreateTransactionCommand() { }

    public CreateTransactionCommand(string type, Guid fromAccountId, Guid toAccountId,
                                   Money amount, Guid? userId = null,
                                   string? item = null, string? detail = null, string? paymentMethod = null)
    {
        Type = type;
        FromAccountId = fromAccountId;
        ToAccountId = toAccountId;
        Amount = amount;
        Item = item;
        Detail = detail;
        PaymentMethod = paymentMethod;
        UserId = userId;
    }
}