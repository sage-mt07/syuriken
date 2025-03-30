# KSQL EntityFramework Guidelines

## Project Information
- .NET 8.0 C# library
- EntityFramework-like API for KSQL/Kafka Streams

## Build Commands
```bash
# Build the project
dotnet build

# Run tests (when added)
dotnet test 
```

## Code Style Guidelines

### Naming Conventions
- **Classes**: PascalCase (e.g., `KsqlDbContext`)
- **Interfaces**: PascalCase with 'I' prefix (e.g., `IKsqlStream<T>`)
- **Methods**: PascalCase (e.g., `ProduceAsync`)
- **Properties**: PascalCase (e.g., `CustomerId`)
- **Variables**: camelCase

### Structure Guidelines
- Use attributes for declarative design (`[Topic]`, `[Key]`, etc.)
- Support both attribute-based and Fluent API configurations
- Async methods using Task/ValueTask with -Async suffix
- Use `DateTimeOffset` over `DateTime` for timestamp information
- Specify decimal precision explicitly with attributes
- Define key properties with `[Key]` attribute

### Error Handling
- Use policy-based error handling (`ErrorPolicy.Skip`, etc.)
- Implement dead letter queues for failed messages
- Apply retry mechanisms for transient failures

### Documentation
- Use standard XML comments for all public APIs
- Follow topic-centric design approach
- Document schema compatibility and timestamp columns