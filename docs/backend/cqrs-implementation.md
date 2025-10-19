# CQRS Implementation Guide

## Overview

This project uses a custom CQRS (Command Query Responsibility Segregation) implementation with a mediator pattern. This approach separates read operations (queries) from write operations (commands), providing clear separation of concerns and better maintainability.

## Architecture

### Why Roll Our Own?

MediatR became a paid library. Rather than adopting another dependency, we implemented a lightweight, custom solution that:

- Provides the same core functionality as MediatR
- Supports pipeline behaviors for cross-cutting concerns
- Has zero licensing concerns
- Gives complete control over the implementation
- Integrates cleanly with Clean Architecture

### Components Location

```
src/
├── Application/
│   ├── Common/
│   │   ├── CQRS/              # Core CQRS interfaces
│   │   │   ├── ICommand.cs
│   │   │   ├── IQuery.cs
│   │   │   ├── ICommandHandler.cs
│   │   │   ├── IQueryHandler.cs
│   │   │   ├── IMediator.cs
│   │   │   ├── IPipelineBehavior.cs
│   │   │   └── Unit.cs
│   │   ├── Behaviors/         # Pipeline behaviors
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── PerformanceBehavior.cs
│   │   └── Exceptions/
│   │       └── ValidationException.cs
│   ├── Commands/              # All command handlers
│   │   └── Identity/
│   │       └── CreateUser/
│   │           ├── CreateUserCommand.cs
│   │           ├── CreateUserCommandHandler.cs
│   │           └── CreateUserCommandValidator.cs
│   ├── Queries/               # All query handlers
│   │   └── Identity/
│   │       └── GetUserById/
│   │           ├── GetUserByIdQuery.cs
│   │           └── GetUserByIdQueryHandler.cs
│   └── DTOs/                  # Data transfer objects
│
├── Infrastructure/
│   └── Mediator/
│       └── Mediator.cs        # Mediator implementation
│
└── Api/
    ├── Controllers/           # Use mediator here
    │   └── UsersController.cs
    └── Extensions/
        └── MediatorServiceExtensions.cs
```

## Key Interfaces

### ICommand

Commands represent write operations that modify state:

```csharp
// Command with return value
public interface ICommand<out TResponse> { }

// Command without return value (returns Unit)
public interface ICommand : ICommand<Unit> { }
```

### IQuery

Queries represent read operations without side effects:

```csharp
public interface IQuery<out TResponse> { }
```

### ICommandHandler / IQueryHandler

Handlers execute the actual business logic:

```csharp
public interface ICommandHandler<in TCommand, TResponse> 
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<in TQuery, TResponse> 
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

### IMediator

The mediator dispatches commands and queries to their handlers:

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
```

## Creating Commands

### 1. Define the Command

```csharp
// src/Application/Commands/Identity/CreateUser/CreateUserCommand.cs
public record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null
) : ICommand<Guid>;
```

### 2. Create the Handler

```csharp
// src/Application/Commands/Identity/CreateUser/CreateUserCommandHandler.cs
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PhoneNumber = command.PhoneNumber
        };

        await _userRepository.AddAsync(user);
        return user.Id;
    }
}
```

### 3. Add Validation (Optional)

```csharp
// src/Application/Commands/Identity/CreateUser/CreateUserCommandValidator.cs
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters");
    }
}
```

## Creating Queries

### 1. Define the Query

```csharp
// src/Application/Queries/Identity/GetUserById/GetUserByIdQuery.cs
public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>;
```

### 2. Create the Handler

```csharp
// src/Application/Queries/Identity/GetUserById/GetUserByIdQueryHandler.cs
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId);
        
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
```

## Using in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.QueryAsync(query, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            var userId = await _mediator.SendAsync(command, cancellationToken);

            return CreatedAtAction(nameof(GetUser), new { id = userId }, new { id = userId });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}
```

## Pipeline Behaviors

Pipeline behaviors are cross-cutting concerns that execute before and after handlers.

### Available Behaviors

1. **LoggingBehavior** - Logs command/query execution
2. **ValidationBehavior** - Validates requests using FluentValidation
3. **PerformanceBehavior** - Logs warnings for slow requests (>500ms)

### Execution Order

Behaviors execute in registration order:

```csharp
// In MediatorServiceExtensions.cs
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
```

Execution flow:
1. LoggingBehavior (start)
2. ValidationBehavior (validates)
3. PerformanceBehavior (measures)
4. Handler (executes)
5. PerformanceBehavior (logs if slow)
6. ValidationBehavior (complete)
7. LoggingBehavior (complete)

### Creating Custom Behaviors

```csharp
public class CustomBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Before handler execution
        
        var response = await next();
        
        // After handler execution
        
        return response;
    }
}
```

Register in `Program.cs`:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CustomBehavior<,>));
```

## Registration

All handlers are automatically registered by convention in `Program.cs`:

```csharp
builder.Services.AddMediator();
```

This extension method:
- Registers the mediator
- Scans the Application assembly for all handlers
- Registers all command and query handlers
- Registers all FluentValidation validators
- Registers all pipeline behaviors

## Best Practices

### Naming Conventions

- **Commands**: `{Verb}{Entity}Command` (e.g., `CreateUserCommand`, `UpdateProfileCommand`)
- **Queries**: `Get{Entity}By{Criteria}Query` (e.g., `GetUserByIdQuery`, `GetUsersByTenantQuery`)
- **Handlers**: `{CommandOrQueryName}Handler` (e.g., `CreateUserCommandHandler`)
- **Validators**: `{CommandName}Validator` (e.g., `CreateUserCommandValidator`)

### File Organization

Organize by feature, not by type:

```
Commands/
  Identity/
    CreateUser/
      CreateUserCommand.cs
      CreateUserCommandHandler.cs
      CreateUserCommandValidator.cs
    UpdateUser/
      UpdateUserCommand.cs
      UpdateUserCommandHandler.cs
      UpdateUserCommandValidator.cs
```

### Command vs Query Guidelines

**Use Commands for:**
- Creating, updating, or deleting data
- Operations with side effects
- Business logic that modifies state

**Use Queries for:**
- Reading data
- No side effects
- Projections and transformations
- Reports and analytics

### Error Handling

Handle exceptions in controllers, not handlers:

```csharp
try
{
    var result = await _mediator.SendAsync(command);
    return Ok(result);
}
catch (ValidationException ex)
{
    return BadRequest(new { errors = ex.Errors });
}
catch (NotFoundException ex)
{
    return NotFound(new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception");
    return StatusCode(500, new { message = "Internal server error" });
}
```

## Testing

### Testing Handlers Directly

```csharp
public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsUserId()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        var handler = new CreateUserCommandHandler(mockRepo.Object);
        var command = new CreateUserCommand("test@example.com", "John", "Doe");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        mockRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }
}
```

### Testing Through Mediator

```csharp
public class UsersControllerTests
{
    [Fact]
    public async Task CreateUser_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        var expectedId = Guid.NewGuid();
        mockMediator
            .Setup(x => x.SendAsync(It.IsAny<CreateUserCommand>(), default))
            .ReturnsAsync(expectedId);

        var controller = new UsersController(mockMediator.Object, Mock.Of<ILogger<UsersController>>());
        var request = new CreateUserRequest("test@example.com", "John", "Doe");

        // Act
        var result = await controller.CreateUser(request, default);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(expectedId, ((dynamic)createdResult.Value).id);
    }
}
```

## Additional Features

### Commands Without Return Values

Use `ICommand` (without type parameter) for commands that don't return a value:

```csharp
public record DeleteUserCommand(Guid UserId) : ICommand;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        // Delete logic
    }
}

// In controller
await _mediator.SendAsync(new DeleteUserCommand(userId));
```

### Validation

FluentValidation is automatically integrated through the `ValidationBehavior`. Any validators in the Application assembly are automatically discovered and executed.

If validation fails, a `ValidationException` is thrown with a dictionary of errors:

```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["Email is required", "Email must be a valid email address"],
    "FirstName": ["First name is required"]
  }
}
```

## Comparison with MediatR

| Feature | Our Implementation | MediatR |
|---------|-------------------|---------|
| Command/Query Pattern | ✅ | ✅ |
| Pipeline Behaviors | ✅ | ✅ |
| Validation Support | ✅ | ✅ |
| Async Support | ✅ | ✅ |
| License | Free | Paid |
| Control | Complete | Limited |
| Dependencies | 0 (excl. FluentValidation) | 1+ |
| Performance | Reflection-based | Reflection-based |

## Future Enhancements

Possible additions:

1. **Caching Behavior** - Cache query results
2. **Transaction Behavior** - Wrap commands in database transactions
3. **Authorization Behavior** - Check permissions before execution
4. **Audit Behavior** - Log all command executions
5. **Retry Behavior** - Retry failed commands
6. **Source Generators** - Eliminate reflection for better performance

