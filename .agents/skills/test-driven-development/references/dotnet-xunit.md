# TDD in .NET with xUnit

## Project layout and naming

One test project per production project (`MyApp.Tests` next to `MyApp`), one test class per class under test, named `<ClassUnderTest>Tests`. Name test methods for the scenario and expectation, not the method call:

```csharp
// Preferred
public void CreateUser_WithDuplicateEmail_ReturnsConflict()
public void Should_RejectUpload_When_ChecksumMismatch()

// Avoid - describes implementation, not behavior
public void TestCreateUser1()
public void CallsRepositorySaveAsync()
```

## AAA with xUnit

```csharp
public class SpeedConversionServiceTests
{
    [Fact]
    public void ConvertToMph_ZeroKmh_ReturnsZero()
    {
        // Arrange
        var sut = new SpeedConversionService();

        // Act
        var result = sut.ConvertToMph(0);

        // Assert
        Assert.Equal(0, result);
    }
}
```

Share Arrange logic across tests via the constructor (xUnit creates a fresh instance per test method, so constructor state never leaks between tests) rather than `[SetUp]`-style shared mutable fixtures. Implement `IDisposable` for teardown if a test allocates something that needs cleanup (temp files, connections).

## Theory/InlineData for the "next test forces generalization" step

This is exactly the triangulation mechanic red-green-refactor relies on - each new input is a new failing test until the implementation is genuinely general, not merely correct for the first example:

```csharp
[Theory]
[InlineData(0, false)]   // not prime
[InlineData(1, false)]   // not prime
[InlineData(2, true)]    // smallest prime
[InlineData(17, true)]
[InlineData(18, false)]
public void IsPrime_ReturnsExpectedResult(int candidate, bool expected)
{
    var sut = new PrimeService();
    Assert.Equal(expected, sut.IsPrime(candidate));
}
```

Use `[InlineData]` for primitive literals, `[MemberData]`/`[ClassData]` when the test data is a complex object or shared across classes, and `TheoryData<T>` for type-safe rows without `object[]` boxing. If a `[Theory]` needs more than three or four parameters to make sense, that's usually a sign the test (or the method under test) is doing too much - split it.

## Testing async code and minimal APIs

xUnit runs `async Task` test methods natively - no special attribute needed:

```csharp
[Fact]
public async Task GetFileAsync_MissingBlob_ReturnsNotFound()
{
    var sut = new FileEndpointHandler(_fakeBlobStore);
    var result = await sut.GetFileAsync(FileId.New());
    Assert.IsType<NotFound>(result);
}
```

For minimal API endpoint handlers, test the handler delegate/method directly against fakes for its dependencies rather than spinning up the full `WebApplicationFactory` for every case - reserve `WebApplicationFactory`-based integration tests for the handful of scenarios that specifically need routing, middleware, or the `PolicyScheme` auth pipeline to be exercised end to end (e.g. confirming the LDAP/SyncApi/Entra ID scheme selection actually routes to the right handler).

## Strongly-typed IDs, Dapper, and EF Core together

When a project mixes EF Core (`ValueConverter`) and Dapper (`TypeHandler`) against the same `readonly record struct` ID type, write the conversion round-trip as its own small, fast unit test independent of any database:

```csharp
[Theory]
[InlineData("018f2c9a-1234-7000-8000-000000000001")]
public void FileId_TypeHandler_RoundTripsThroughGuid(string guid)
{
    var id = new FileId(Guid.Parse(guid));
    var parameter = new FileIdTypeHandler().Parse(id.Value);
    Assert.Equal(id, new FileId(parameter));
}
```

Keep this separate from any test that touches a real or in-memory database - a bug in the converter should fail fast, without waiting on database fixture setup. Reserve database-backed tests (EF Core `InMemory`/SQLite for fast checks, a real SQL Server instance via Testcontainers for anything touching `NodeClosure` recursive queries or `sp_Acl_BreakInheritance`) for behavior that genuinely can't be verified without the database engine's semantics.

## Isolating boundaries: avoid a mocking library by default

Reach for `Mock<T>()` last, not first. A mocking library makes it too easy to mock things that don't need mocking, and every `.Setup(...)` call is a little bit of hidden fake behavior baked into the test instead of visible, debuggable code. Work down this list and stop at the first option that fits:

**1. No double at all.** If the collaborator is pure logic you own (a mapper, a calculator, a domain rule), just construct the real thing. This is the common case for most of the domain layer and should need no justification.

**2. A hand-written fake.** For a repository-shaped interface, a small in-memory implementation is usually less code than the equivalent `Setup(...)` calls, and it behaves like the real thing instead of returning whatever you told it to:

```csharp
public class FakeFileRepository : IFileRepository
{
    private readonly Dictionary<FileId, FileRecord> _store = new();

    public Task<FileRecord?> GetAsync(FileId id) =>
        Task.FromResult(_store.GetValueOrDefault(id));

    public Task SaveAsync(FileRecord record)
    {
        _store[record.Id] = record;
        return Task.CompletedTask;
    }
}
```

```csharp
var repo = new FakeFileRepository();
var sut = new FileService(repo);

var result = await sut.GetAsync(fileId);

Assert.True(result.IsNotFound);
```

You can set a breakpoint in it, reuse it across every test in the class, and if you save then read you actually get back what you saved - a mock would happily let that invariant drift out of sync with reality. Write the fake once per interface and share it; the up-front cost pays for itself after the second or third test.

**3. The real infrastructure, made fast and isolated.** Often the "real thing" is available in a form cheap enough for unit tests, and it removes any risk that the fake's behavior has quietly diverged from the real implementation:

- `TimeProvider` / `FakeTimeProvider` (built into .NET 8+) instead of mocking `DateTime.Now` - injects a controllable clock without a mocking library.
- EF Core `InMemory` or SQLite for straightforward repository queries.
- Testcontainers with a real SQL Server for anything that depends on actual engine semantics - recursive CTEs over `NodeClosure`, `sp_Acl_BreakInheritance`.
- Azurite for Blob storage-touching tests instead of mocking `IBlobStorageClient`.
- A fake `HttpMessageHandler` (or `Microsoft.Extensions.Http`'s testing helpers) for the Graph API client, instead of mocking a wrapper around it.

**4. A mocking library, as the last resort.** Occasionally justified: a third-party interface you don't control is too wide to fake by hand for one test, or you genuinely need to verify an interaction that leaves no other observable trace (e.g. confirming an `IUnitOfWork` commit fired after a specific HTTP status, when the commit itself is the whole point of the middleware being tested):

```csharp
var uow = new Mock<IUnitOfWork>();
var sut = new StatusCodeCommitMiddleware(uow.Object);

await sut.InvokeAsync(contextWithStatus(200));

uow.Verify(u => u.CommitAsync(), Times.Once);
```

Even here, verify the outcome (return value, thrown exception, persisted state) before reaching for `Verify(...)` on the mock itself - interaction verification is for the rare case where there's genuinely nothing else to assert on, not the default way of checking a test passed. If you notice most of your tests for a class end up here, that's usually telling you the class has too many collaborators, not that mocking is unavoidable.

## Mutation testing with Stryker.NET

Coverage tells you a line executed; it doesn't tell you the test would fail if the line were wrong. Stryker.NET closes that gap by mutating your production code (flipping `>` to `>=`, `+` to `-`, removing a null check) and re-running your suite - a mutant that survives means some change to that logic wouldn't be caught by any test.

```bash
dotnet tool install -g dotnet-stryker
dotnet stryker --project MyApp.csproj
```

Minimal `stryker-config.json`:

```json
{
  "stryker-config": {
    "project": "../MyApp/MyApp.csproj",
    "test-projects": ["MyApp.Tests.csproj"],
    "mutation-level": "Standard",
    "thresholds": { "high": 80, "low": 60, "break": 50 }
  }
}
```

Don't chase 100%. Run it selectively on business-critical or genuinely risky modules (ACL permission evaluation, chunked-upload integrity checks, auth scheme routing) rather than the whole solution on every PR - a full run executes the entire suite once per mutant and gets slow fast. A weekly scheduled run on the critical modules, or a run before a release, is a reasonable cadence. Treat a surviving mutant as a prompt to add or strengthen an assertion, not as something to chase to zero.
