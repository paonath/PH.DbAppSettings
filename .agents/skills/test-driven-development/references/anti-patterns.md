# Test anti-patterns and smells

Read this before adding mocks, stubs, or any test infrastructure - and whenever asked to review whether an existing test suite is trustworthy. These patterns recur across languages; examples below lean C#/TypeScript since that's the common stack, but the shape of each problem is universal.

## Two schools, and which default to use

- **Classicist / Detroit style** - use real objects wherever practical; reach for a test double only when the real thing is awkward (a database, a clock, a network call). Tests exercise real collaborators, so a passing suite gives confidence that the actual object graph works together.
- **Mockist / London style** - isolate the unit under test completely; every collaborator with behavior gets a mock, and the test verifies interactions (which methods were called, with what arguments) rather than end state.

Default to **classicist**. It produces tests that survive refactors, because they check outcomes rather than call sequences. Reach for London-style interaction testing only when you specifically need to verify that a side effect happened (an email was queued, an event was published) and there's no observable state to assert on instead. Mixing both freely in the same test class is fine; defaulting to mock-everything is the anti-pattern.

## The mocking rule of thumb: avoid it, don't just aim it

The goal isn't "mock the right things" - it's **use as little mocking as the problem allows**. Every mock is a small, hand-authored lie about how a dependency behaves; the more of them a test contains, the more it's testing your assumptions about the world instead of your code. Before reaching for a mocking library, work down this list and stop at the first option that fits:

1. **No double at all.** Your own internal domain objects, value types, calculators, mappers - construct real instances. This covers most of what a codebase actually contains.
2. **A small hand-written fake.** For a repository-shaped or client-shaped interface, an in-memory implementation is usually less code than the equivalent mock setup, behaves consistently across every test that uses it, and you can put a breakpoint in it.
3. **The real thing, made fast and isolated.** An in-memory/SQLite database, a fake clock (`TimeProvider` in .NET, injectable clocks elsewhere), a local container for external infra (Testcontainers, Azurite) - genuinely exercising the real behavior instead of a stand-in for it, at a cost that's still fine for a unit test.
4. **A mocking library, only when the above are impractical** - typically a wide third-party interface you don't control, or a side effect with no other observable trace to assert on.

Mock **true external boundaries only** if you do reach for a mock: database/repository calls, HTTP clients to other services, the filesystem, the system clock, randomness/GUID generation, message queues. Do **not** mock:

- Your own internal domain objects, value types, or simple calculators - construct real instances.
- Data structures (DTOs, records) - they have no behavior worth faking.
- A dependency purely to avoid setup work - if setup is painful, that's signal about the design (see Excessive Setup below), not a reason to mock it away.

## Catalog

**The Mockery.** A test built almost entirely out of mocks, stubs, and fakes, to the point where the assertions check what the mocks returned rather than what the system under test actually did. The test passes and proves nothing: it validates your test double's configuration, not your code.

```csharp
// BAD - every collaborator mocked, assertion checks mock arithmetic
var mapper = new Mock<IUserMapper>();
var validator = new Mock<IUserValidator>();
mapper.Setup(m => m.Map(It.IsAny<CreateUserRequest>())).Returns(new User("alice"));
validator.Setup(v => v.Validate(It.IsAny<User>())).Returns(true);
// ...
mapper.Verify(m => m.Map(It.IsAny<CreateUserRequest>()), Times.Once);
```

Fix: use the real mapper and validator (they're your own internal code with no external dependency of their own), and assert on the actual saved/returned user.

**The Inspector.** A test that reaches into private state or internal methods to achieve high coverage. It knows so much about the object's internals that any refactor - even one that preserves behavior exactly - breaks the test and forces a matching test change. If you need to violate encapsulation to write the assertion, the test is aimed at the wrong level; assert on the public contract instead.

**The Liar (a.k.a. Success Against All Odds).** A test that passes regardless of whether the code is correct - usually because it asserts nothing meaningful, catches and swallows an exception that should have failed the test, or was never watched failing in the first place. Any bug introduced later sails through undetected. The only fix is to delete it and write a real one; a liar is worse than no test, because it creates false confidence.

**The Giant.** A single test method that exercises many behaviors and contains many assertions, usually because several small tests got merged "for efficiency." When it fails, you can't tell which behavior broke without reading the whole thing. Split it: one behavior, one Act step, per test.

**Excessive Setup.** Testing one specific behavior requires constructing a long chain of unrelated objects and mocks first. This is a design smell more than a testing smell - it usually means the class under test has too many dependencies or too little separation of concerns. Prefer fixing the design (smaller constructors, factory/builder helpers for test data) over normalizing a fifty-line Arrange block.

**The Stranger.** The test digs into another object's internals to set up its own fixture (e.g., reaching through `person.Address.ZipCode.Value` three levels deep to construct test data). Hide that behind a builder or factory method instead.

**Generous Leftovers (a.k.a. Chain Gang).** One test persists data that another test depends on and reuses, so the second test only passes if the first one ran (and ran first). Breaks the Independent property from FIRST. Each test should create and, if needed, clean up its own data.

**The Local Hero / Operating System Evangelist.** The test only passes on the machine (or OS) it was written on, usually because of a hardcoded path, timezone assumption, or an undeclared dependency on local environment state. Given the Mac/Windows-Parallels split common in this workflow, watch specifically for hardcoded path separators, drive letters, or line-ending assumptions leaking into test fixtures.

**The Free Ride / Piggyback.** A new assertion gets tacked onto an existing, unrelated test instead of getting its own test, because "it's already set up." Makes failures ambiguous and hides which behavior actually broke. Give the new behavior its own test even if the Arrange step looks similar.

**Flaky / time-oriented tests.** A test that depends on `DateTime.Now`, real delays, or network timing will pass today and fail on a different day or under load. Inject a clock abstraction (or freeze time with the test framework's fake-timer support) instead of calling the system clock directly from code under test.

**Assertion Roulette.** Multiple assertions in one test with no failure messages, so a failure tells you *that* something broke but not *what*. Prefer one logical assertion per test, or use assertion libraries that produce a clear message per check (FluentAssertions, Jasmine/Jest matchers) if multiple checks genuinely belong together.

## A quick self-check before adding a mock

1. Is the thing I'm about to mock an external boundary (DB, HTTP, filesystem, clock, randomness)? If not, use the real object.
2. After this test passes, will it still catch a real regression if someone reintroduces the bug? If the only thing failing would be the mock's own configuration, it's not testing anything.
3. Am I asserting on the outcome (return value, saved state, thrown exception), or on the fact that a mock was called? Prefer outcome-based assertions; reserve interaction verification (`mock.Verify(...)`, `expect(spy).toHaveBeenCalled()`) for side effects that have no other observable trace.
