# DDD Architecture Guide - TNGFinHack Server

## Overview

This project implements **Domain-Driven Design (DDD)** principles to create a scalable, maintainable, and testable backend architecture. The system is organized into four distinct layers:

## Architecture Layers

### 1. **Domain Layer** (`/Domain/`)
The core of the application containing business logic and rules.

#### Components:
- **Entities**: `User`, `Transaction`, `Budget`, `Wallet`
  - Encapsulate business logic and invariants
  - Rich models with behavior, not just data containers
  - Example: `User` validates password complexity, `Money` prevents negative amounts

- **Value Objects**: `Money`
  - Immutable objects representing concepts without identity
  - Ensure domain consistency (e.g., currency validation)
  - Example: `Money(100, "MYR")` cannot be modified after creation

- **Services**: `TransactionDomainService`
  - Implement business rules that don't belong to a single entity
  - Stateless operations across multiple domain objects
  - Example: Transfer validation uses business logic from multiple entities

- **Interfaces**: `IUserRepository`, `ITransactionRepository`, etc.
  - Define contracts that implementations must follow
  - Enable dependency inversion and testability
  - Implementation lives in Infrastructure layer

- **Exceptions**: Custom exception types for domain violations
  - `ValidationException`: Input validation failures
  - `InsufficientFundsException`: Business rule violations
  - `EntityNotFoundException`: Missing required data

### 2. **Application Layer** (`/Application/`)
Orchestrates domain logic and coordinates with infrastructure.

#### Components:
- **Commands**: `CreateTransactionCommand`, `RegisterUserCommand`
  - Request objects containing operation parameters
  - DTO-like objects but strongly typed and immutable
  - Example: `new CreateTransactionCommand(fromId, toId, amount, userId)`

- **Application Services**: `TransactionApplicationService`, `WalletApplicationService`
  - Coordinate domain and infrastructure operations
  - Translate external requests into domain operations
  - Handle transaction management and persistence
  - Example: Validate user exists, execute transfer, record transaction

- **DTOs**: `TransactionDto`, `CreateTransactionRequest`
  - Data transfer objects for API communication
  - Separate from domain entities to prevent API leakage
  - Example: API accepts `CreateTransactionRequest`, not domain `Transaction`

- **Results**: `ValidationResult` for operation outcomes
  - Strongly typed results instead of throwing exceptions
  - Support for multiple errors in single operation
  - Example: Validate budget before transaction

### 3. **Infrastructure Layer** (`/Infrastructure/`)
Technical implementations of domain abstractions.

#### Components:
- **Repositories**: `EfUserRepository`, `EfTransactionRepository`
  - Implement domain repository interfaces
  - Handle database operations using Entity Framework
  - Map between domain entities and database models
  - Example: Query PostgreSQL using `IUserRepository.GetByEmailAsync()`

- **Middleware**: `GlobalExceptionHandlingMiddleware`
  - Cross-cutting concerns for the entire application
  - Convert domain exceptions to HTTP responses
  - Example: `ValidationException` → 400 Bad Request

- **External Services**: Stripe, Bank APIs
  - Adapt external systems to domain concepts
  - Example: `IPaymentGatewayClient` abstracts Stripe details

### 4. **Presentation Layer** (`/Controllers/`)
HTTP endpoints and request handling.

#### Components:
- **Controllers**: `AuthController`, `TransactionController`
  - Accept HTTP requests
  - Delegate to application services
  - Return HTTP responses
  - Example: POST `/api/transactions` → `TransactionApplicationService.CreateTransactionAsync()`

## Key Design Patterns

### 1. **Repository Pattern**
```csharp
// Domain defines the interface
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
}

// Infrastructure implements it
public class EfUserRepository : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        var efUser = await _db.Users.FindAsync(id);
        return efUser == null ? null : MapToDomain(efUser);
    }
}

// Application uses it
var user = await _userRepository.GetByIdAsync(userId);
```

### 2. **Value Objects**
```csharp
// Ensures type safety and prevents invalid states
var money = new Money(100, "MYR");
var newBalance = money.Add(new Money(50)); // Valid
// money.Amount = -50; // Compile error - immutable

// Automatic validation
var invalid = new Money(-100); // Throws ArgumentException
```

### 3. **Domain Services**
```csharp
// Business logic that spans multiple entities
public class TransactionDomainService
{
    public async Task ExecuteTransferAsync(Guid from, Guid to, Money amount)
    {
        // Validates business rules
        // Updates multiple entities atomically
        // Doesn't know about HTTP or database details
    }
}
```

### 4. **Command Pattern**
```csharp
// Explicit, self-documenting operations
public class CreateTransactionCommand
{
    public Guid FromAccountId { get; set; }
    public Guid ToAccountId { get; set; }
    public Money Amount { get; set; }
}

// Application service handles the command
var result = await _transactionService.CreateTransactionAsync(command);
```

## Adding New Features

### Step 1: Define Domain Entities & Interfaces
```csharp
// 1. Create domain entity in Domain/Entities/
public class Loan
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public void RepayAmount(Money payment) { /* ... */ }
}

// 2. Define repository interface in Domain/Interfaces/
public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id);
    Task AddAsync(Loan loan);
    Task UpdateAsync(Loan loan);
}
```

### Step 2: Implement Repository
```csharp
// Infrastructure/Repositories/EfLoanRepository.cs
public class EfLoanRepository : ILoanRepository
{
    public async Task<Loan?> GetByIdAsync(Guid id)
    {
        var efLoan = await _db.Loans.FindAsync(id);
        return efLoan == null ? null : MapToDomain(efLoan);
    }
    // ... other methods
}
```

### Step 3: Create Application Service
```csharp
// Application/Services/LoanApplicationService.cs
public class LoanApplicationService
{
    private readonly ILoanRepository _loanRepository;

    public async Task<Loan> CreateLoanAsync(CreateLoanCommand command)
    {
        // Validate
        // Create domain entity
        // Persist
        var loan = new Loan(Guid.NewGuid(), command.Amount);
        await _loanRepository.AddAsync(loan);
        return loan;
    }
}
```

### Step 4: Register in DI Container
```csharp
// Program.cs
builder.Services.AddScoped<ILoanRepository, EfLoanRepository>();
builder.Services.AddScoped<LoanApplicationService>();
```

### Step 5: Create Controller Endpoint
```csharp
// Controllers/LoanController.cs
[ApiController]
[Route("api/loans")]
public class LoanController : ControllerBase
{
    private readonly LoanApplicationService _loanService;

    [HttpPost]
    public async Task<IResult> CreateLoan([FromBody] CreateLoanRequest request)
    {
        var command = new CreateLoanCommand(request.Amount);
        var loan = await _loanService.CreateLoanAsync(command);
        return Results.Created($"/api/loans/{loan.Id}", loan);
    }
}
```

## Testing Strategy

### Unit Tests (Domain Logic)
```csharp
[TestClass]
public class TransactionTests
{
    [TestMethod]
    public void CannotTransferNegativeAmount()
    {
        // Arrange
        var money = new Money(100);
        
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => new Money(-50)
        );
    }
}
```

### Integration Tests (Repositories)
```csharp
[TestClass]
public class UserRepositoryTests
{
    private AppDbContext _db;
    private EfUserRepository _repo;

    [TestInitialize]
    public void Setup()
    {
        _db = new AppDbContext(options);
        _repo = new EfUserRepository(_db);
    }

    [TestMethod]
    public async Task GetByEmailAsync_ReturnsUser()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "John", "hash", "123456", "john@example.com");
        await _repo.AddAsync(user);

        // Act
        var result = await _repo.GetByEmailAsync("john@example.com");

        // Assert
        Assert.IsNotNull(result);
    }
}
```

### API Tests (E2E)
```csharp
[TestClass]
public class TransactionApiTests
{
    [TestMethod]
    public async Task PostTransaction_ReturnsCreated()
    {
        // Arrange
        var client = new HttpClient { BaseAddress = _server.BaseAddress };
        var request = new { TransactionFrom = "...", TransactionTo = "...", Amount = 100 };

        // Act
        var response = await client.PostAsJsonAsync("/api/transactions", request);

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }
}
```

## Database Mapping

The domain entities are separate from EF models to prevent database concerns from leaking into domain logic.

```csharp
// Domain/Entities/User.cs - Pure business logic
public class User { /* ... */ }

// Models/User.cs - EF model for database
[Table("users")]
public class User { /* ... */ }

// Repositories map between them
public User MapToDomain(Models.User efUser)
{
    return new User(efUser.UserId, efUser.UserName, ...);
}
```

## Error Handling

All domain and application layer exceptions are caught by the global exception middleware and converted to standardized HTTP responses:

```csharp
// Thrown in domain/application
throw new ValidationException("Invalid amount");

// Converted to HTTP response by middleware
{
    "statusCode": 400,
    "message": "Invalid amount",
    "errorCode": "VALIDATION_ERROR",
    "timestamp": "2026-04-25T10:30:00Z"
}
```

## Benefits of This Architecture

✅ **Testability**: Domain logic can be tested without database or HTTP  
✅ **Maintainability**: Clear separation of concerns, easy to locate changes  
✅ **Flexibility**: Swap database, payment gateway, or notification service  
✅ **Scalability**: Easy to add new features without affecting existing code  
✅ **Domain Focus**: Business logic is prominent and easy to understand  
✅ **Database Agnostic**: Switch from PostgreSQL to MongoDB without changing domain  
✅ **Type Safety**: Value objects prevent invalid states at compile time  
✅ **Documentation**: Code structure itself explains the business domain  

## Common Pitfalls to Avoid

❌ **Domain depends on infrastructure**: Never use `DbContext` in domain entities  
❌ **Anemic models**: Entities should have behavior, not just properties  
❌ **Mixing layers**: Don't put business logic in controllers  
❌ **Generic repositories**: Make repositories express the ubiquitous language  
❌ **Skipping value objects**: Use them for concepts that benefit from type safety  
❌ **Over-engineering**: Start simple and add patterns as complexity grows  

## References

- [Domain-Driven Design by Eric Evans](https://domainlanguage.com/ddd/)
- [Implementing DDD by Vaughn Vernon](https://vaughnvernon.com/implementing-ddd/)
- [Microsoft - DDD in C#](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns)
