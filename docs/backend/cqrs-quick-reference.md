# CQRS Quick Reference

## Creating a New Command

```csharp
// 1. Define Command
public record CreateSomethingCommand(string Name, string Description) : ICommand<Guid>;

// 2. Create Handler
public class CreateSomethingCommandHandler : ICommandHandler<CreateSomethingCommand, Guid>
{
    private readonly ISomethingRepository _repository;

    public CreateSomethingCommandHandler(ISomethingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(CreateSomethingCommand command, CancellationToken cancellationToken = default)
    {
        var entity = new Something { Name = command.Name, Description = command.Description };
        await _repository.AddAsync(entity);
        return entity.Id;
    }
}

// 3. Add Validator (Optional)
public class CreateSomethingCommandValidator : AbstractValidator<CreateSomethingCommand>
{
    public CreateSomethingCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
```

## Creating a New Query

```csharp
// 1. Define Query
public record GetSomethingByIdQuery(Guid Id) : IQuery<SomethingDto?>;

// 2. Create Handler
public class GetSomethingByIdQueryHandler : IQueryHandler<GetSomethingByIdQuery, SomethingDto?>
{
    private readonly ISomethingRepository _repository;

    public GetSomethingByIdQueryHandler(ISomethingRepository repository)
    {
        _repository = repository;
    }

    public async Task<SomethingDto?> HandleAsync(GetSomethingByIdQuery query, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(query.Id);
        if (entity == null) return null;

        return new SomethingDto { Id = entity.Id, Name = entity.Name };
    }
}
```

## Using in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class SomethingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SomethingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.QueryAsync(new GetSomethingByIdQuery(id), cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSomethingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateSomethingCommand(request.Name, request.Description);
            var id = await _mediator.SendAsync(command, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id }, new { id });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}
```

## File Structure

```
Application/
└── Commands/          OR    Queries/
    └── {Domain}/            └── {Domain}/
        └── {Feature}/           └── {Feature}/
            ├── {Feature}Command.cs          ├── {Feature}Query.cs
            ├── {Feature}CommandHandler.cs   └── {Feature}QueryHandler.cs
            └── {Feature}CommandValidator.cs
```

## Command Without Return Value

```csharp
public record DeleteSomethingCommand(Guid Id) : ICommand;

public class DeleteSomethingCommandHandler : ICommandHandler<DeleteSomethingCommand>
{
    public async Task HandleAsync(DeleteSomethingCommand command, CancellationToken cancellationToken = default)
    {
        // Delete logic
    }
}

// Usage
await _mediator.SendAsync(new DeleteSomethingCommand(id));
```

## Common Patterns

### List Query

```csharp
public record GetAllSomethingsQuery(int PageNumber, int PageSize) : IQuery<List<SomethingDto>>;
```

### Search Query

```csharp
public record SearchSomethingsQuery(string SearchTerm, Guid TenantId) : IQuery<List<SomethingDto>>;
```

### Update Command

```csharp
public record UpdateSomethingCommand(Guid Id, string Name, string Description) : ICommand<Unit>;
```

### Delete Command

```csharp
public record DeleteSomethingCommand(Guid Id) : ICommand;
```

## Validation Rules

```csharp
// Required
RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");

// Email
RuleFor(x => x.Email).NotEmpty().EmailAddress();

// Length
RuleFor(x => x.Name).MinimumLength(2).MaximumLength(100);

// Conditional
RuleFor(x => x.PhoneNumber)
    .NotEmpty()
    .When(x => x.SendSms);

// Custom
RuleFor(x => x.Email)
    .MustAsync(async (email, ct) => await IsUniqueEmail(email))
    .WithMessage("Email already exists");
```

## Error Handling

```csharp
try
{
    var result = await _mediator.SendAsync(command);
    return Ok(result);
}
catch (ValidationException ex)
{
    return BadRequest(new { message = "Validation failed", errors = ex.Errors });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error executing command");
    return StatusCode(500, new { message = "Internal server error" });
}
```

## Testing

```csharp
// Test Handler Directly
[Fact]
public async Task HandleAsync_ValidCommand_ReturnsId()
{
    var mockRepo = new Mock<ISomethingRepository>();
    var handler = new CreateSomethingCommandHandler(mockRepo.Object);
    var command = new CreateSomethingCommand("Test", "Description");

    var result = await handler.HandleAsync(command);

    Assert.NotEqual(Guid.Empty, result);
    mockRepo.Verify(x => x.AddAsync(It.IsAny<Something>()), Times.Once);
}

// Test Through Mediator
[Fact]
public async Task CreateSomething_ValidRequest_ReturnsCreated()
{
    var mockMediator = new Mock<IMediator>();
    var expectedId = Guid.NewGuid();
    mockMediator.Setup(x => x.SendAsync(It.IsAny<CreateSomethingCommand>(), default))
               .ReturnsAsync(expectedId);

    var controller = new SomethingsController(mockMediator.Object);
    var request = new CreateSomethingRequest("Test", "Description");

    var result = await controller.Create(request, default);

    var createdResult = Assert.IsType<CreatedAtActionResult>(result);
}
```

