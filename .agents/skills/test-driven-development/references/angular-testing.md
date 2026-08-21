# TDD in Angular / TypeScript

## First, check which runner the project actually uses

Angular's default test runner has moved over time: Karma+Jasmine was the long-standing default, many projects migrated to Jest for speed, and current Angular CLI versions default new projects to Vitest. All three run Jasmine-style `describe`/`it`/`expect` syntax (Jest and Vitest are close enough to Jasmine's API that most test bodies barely change), but mocking APIs and config differ:

- **Karma + Jasmine** - `jasmine.createSpyObj(...)`, runs in a real browser via Karma, `ng test` out of the box.
- **Jest** - `jest.fn()`, `jest.spyOn()`, runs in Node with jsdom, needs `jest-preset-angular`.
- **Vitest** - `vi.fn()`, `vi.spyOn()`, runs in Node with jsdom/happy-dom, current Angular CLI default, generally the fastest of the three.

Check `angular.json` / `package.json` before writing a single test - don't assume Karma just because that's the historical default, and don't introduce a second runner into a project that already has one.

## AAA with a standalone component

```typescript
describe('FileUploadButton', () => {
  it('should disable the button while an upload is in progress', () => {
    // Arrange
    const fixture = TestBed.createComponent(FileUploadButton);
    fixture.componentRef.setInput('uploading', true);

    // Act
    fixture.detectChanges();

    // Assert
    const button = fixture.nativeElement.querySelector('button');
    expect(button.disabled).toBe(true);
  });
});
```

For standalone components, `TestBed.configureTestingModule({ imports: [FileUploadButton] })` (import the component itself rather than declaring it) plus whatever providers/mocks the component actually injects - don't drag in the whole app's provider tree for a focused unit test.

## Testing signals

Assert on the signal's current value directly - no special testing API is needed for a plain read:

```typescript
it('should mark the form dirty after the first edit', () => {
  const service = TestBed.inject(UploadFormService);
  expect(service.isDirty()).toBe(false);

  service.setFileName('report.pdf');

  expect(service.isDirty()).toBe(true);
});
```

If a `computed()` or `effect()` depends on change detection to settle, call `fixture.detectChanges()` (or `TestBed.flushEffects()` where the runner supports it) before asserting, the same way you'd flush a promise microtask queue elsewhere.

## Mocking HTTP

Use the testing-only HTTP providers rather than a hand-rolled fake `HttpClient`:

```typescript
TestBed.configureTestingModule({
  providers: [provideHttpClient(), provideHttpClientTesting()],
});

const httpMock = TestBed.inject(HttpTestingController);
service.uploadChunk(chunk).subscribe();

const req = httpMock.expectOne('/api/files/chunks');
expect(req.request.method).toBe('POST');
req.flush({ success: true });

httpMock.verify(); // fails the test if any request went unmatched
```

This mocks the true external boundary (the network) while exercising your real service logic - exactly the classicist default described in `anti-patterns.md`.

## What to mock, what not to

Same hierarchy as the rest of this skill, applied to the frontend: prefer the real thing, then a small hand-written fake, then the testing-provided infrastructure, and only reach for `jest.fn()`/`vi.fn()`/`jasmine.createSpyObj(...)` when nothing else fits.

- **Real thing first**: pure helper functions, small internal services with no I/O, value objects/interfaces - construct or call the real thing. If a helper isn't pure and that's making it awkward to test, make it pure; that's a design improvement, not a reason to reach for a mock.
- **Testing-provided infrastructure next**: `HttpClient` calls via `provideHttpClientTesting()`/`HttpTestingController` (shown above) exercise your real service logic against a controlled network boundary - this is usually a better fit than hand-mocking the service that calls `HttpClient`.
- **A spy/mock, last**: browser/global APIs your code touches directly (`File`, `Blob`, timers), or an Angular service wrapping an external boundary where no testing-provided fake exists. Even then, prefer `spyOn`/`jest.spyOn` on a single method over replacing the whole object, so the rest of the real service still runs.

## Component vs. service tests

Prefer testing business logic in a service with no `TestBed` at all when the logic doesn't actually touch Angular's DI or lifecycle - plain Jasmine/Jest, construct the class, call the method, assert. Reserve `TestBed`/`ComponentFixture` for behavior that genuinely depends on templates, bindings, or change detection (does the button disable, does the `@for` loop render the right count of rows). Testing pure logic through a full component harness adds setup cost for no extra confidence.

## Mutation testing with StrykerJS

Once coverage looks solid, verify the tests actually catch bugs rather than merely executing lines:

```bash
npm install --save-dev @stryker-mutator/core @stryker-mutator/karma-runner
npx stryker init
npx stryker run
```

(swap `@stryker-mutator/karma-runner` for the Jest or Vitest runner package matching whichever test runner the project actually uses, per the check at the top of this file). Minimal config:

```json
{
  "$schema": "./node_modules/@stryker-mutator/core/schema/stryker-schema.json",
  "testRunner": "jest",
  "coverageAnalysis": "perTest",
  "mutate": ["src/**/*.ts", "!src/**/*.spec.ts"],
  "thresholds": { "high": 80, "low": 60, "break": 50 }
}
```

Run it incrementally on changed files for quick feedback (`npx stryker run --incremental`) and reserve a full run for critical modules or before a release - like Stryker.NET, it re-runs the whole suite once per mutant, so it doesn't belong in the fast inner loop.
