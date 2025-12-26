# AGENTS.md

## Build & Test Commands

- Build: `dotnet build MinMe.slnx -c Release`
- Test all: `dotnet test tests/MinMe.Tests -c Release`
- Run single test: `dotnet test tests/MinMe.Tests --filter "FullyQualifiedName~TestMethodName"`
- Full pipeline (build, test, pack): `dotnet fsi build.fsx -- -p build`

## Code Style Guidelines

- **Target**: .NET 10, C# 14 with nullable enabled and implicit usings
- **Namespaces**: File-scoped (`namespace MinMe.Analyzers;`)
- **Naming**: PascalCase for types/methods, \_camelCase for private fields, camelCase for locals
- **Primary constructors**: Preferred for simple classes (`public class Foo(int bar)`)
- **Collection expressions**: Use `[]` syntax for empty/inline collections
- **Pattern matching**: Use `is not { } variable` for null checks with variable binding
- **Type aliases**: Use `using Alias = Namespace.Type;` for OpenXml disambiguation
- **Error handling**: Return error objects (OptimizeError) rather than throwing exceptions
- **Async**: Suffix async methods with `Async`, use `CancellationToken` parameter
- **Tests**: NUnit framework with `[TestFixture]`, `[Test]`, `[TestCaseSource]` attributes
- **XML docs**: Add `<summary>` for public API classes (see OptimizeError.cs pattern)
