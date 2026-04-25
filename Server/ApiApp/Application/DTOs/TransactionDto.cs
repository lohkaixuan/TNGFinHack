// ==================================================
// Program Name   : TransactionDto.cs
// Purpose        : DTOs for transaction operations
// Developer      : AI Assistant
// Created Date   : 25 April 2026
// ==================================================
using System;

namespace ApiApp.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FromAccount { get; set; } = string.Empty;
    public string ToAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MYR";
    public DateTime Timestamp { get; set; }
    public string? Item { get; set; }
    public string? Detail { get; set; }
    public string? Category { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = "success";
    public DateTime LastUpdate { get; set; }
}

public class CreateTransactionRequest
{
    public string TransactionType { get; set; } = "pay";
    public string TransactionFrom { get; set; } = string.Empty;
    public string TransactionTo { get; set; } = string.Empty;
    public decimal TransactionAmount { get; set; }
    public DateTime? TransactionTimestamp { get; set; }
    public string? TransactionItem { get; set; }
    public string? TransactionDetail { get; set; }
    public string? Mcc { get; set; }
    public string? PaymentMethod { get; set; }
    public string? OverrideCategoryCsv { get; set; }
}