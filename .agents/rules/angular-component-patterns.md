---
trigger: model_decision
description: Angular component patterns and conventions for Angular projects
globs: '**/*.ts, **/*.component.ts'
---

## Component Architecture

### Standalone Components (mandatory)

- All new components MUST be standalone (`standalone: true`).
- Use `inject()` function for dependency injection (not constructor injection).
- Use `ChangeDetectionStrategy.OnPush` for all components.
- Use the project-configured prefix for component selectors (check `angular.json` for `prefix`).

### Inline Templates and Styles (mandatory)

- Use inline templates (`template:` in `@Component`) — no separate `.html` files.
- Use inline styles (`styles:` in `@Component`) — no separate `.css/.scss` files.
- Keep templates concise and readable.

### Component Communication

- Use `@Input()` with immutable data patterns for parent-to-child communication.
- Use `@Output()` with `EventEmitter` for child-to-parent communication.
- **MUST NOT** inject parent components directly.
- **MUST NOT** use shared services for UI event propagation between parent and child.
- Keep presentational components pure (only `@Input`/`@Output`, no business logic).

### Signals (Angular 16+)

- Use Signals for reactive state management within components.
- Prefer Signals over `BehaviorSubject` for local component state.

## TypeScript Best Practices

- Use strict TypeScript configuration (`strict: true`).
- Define clear interfaces for data models and service contracts.
- Use `$` suffix for observable variables (`users$`, `isLoading$`).
- Avoid `any` type unless absolutely necessary.
- Use `UPPER_SNAKE_CASE` for true constants.
- Use kebab-case for file names matching the class name.

## RxJS Patterns

- Use `pipe()` for operator composition.
- Always unsubscribe: use `takeUntil`, `takeUntilDestroyed`, or `async` pipe.
- Use `switchMap` for cancellable requests, `mergeMap` for parallel, `concatMap` for sequential.
- Avoid nested subscriptions: use higher-order mapping operators.
- Prefer `async` pipe in templates over manual subscriptions.

## UI Libraries

- Bootstrap 5 and FontAwesome MUST be used directly via CSS classes in HTML.
- Prefer markup like `<button class="btn btn-outline-secondary"><i class="fa-solid fa-minus"></i></button>`.
- Use NgBootstrap only when actual JS behavior is needed (modals, tooltips, datepickers).
- Import NgBootstrap symbols only in components that use them.

## Testing

- Use Jasmine for unit tests with Karma (FirefoxHeadless).
- Use `fakeAsync`/`tick()` for async test scenarios.
- Use Jasmine spies on injected services.
- For promise-based APIs in `fakeAsync`, call `tick()` before assertions.

## Lifecycle and Change Detection

- Implement `ngOnChanges` for reacting to input changes.
- Use `setTimeout` when DOM updates need to happen after Angular's change detection cycle.
- Track selected item ID or similar state across list refreshes for selection continuity.

## Anti-Patterns

- MUST NOT create component files manually: use `ng g c`.
- MUST NOT use separate `.html` or `.css` template/style files.
- MUST NOT declare components in NgModule in new code.
- MUST NOT subscribe in templates without `async` pipe.
- MUST NOT mutate `@Input` values directly.

## Component Layout and Naming Consistency

- When planning new components, always get inspiration from existing components in the project (e.g., an existing dashboard or admin component) to keep layout, padding, grids, and style variables perfectly consistent.
- In case of doubt or architectural/design choices, always initiate a QA with the user before starting implementation.
- All admin components MUST occupy the full horizontal space (`inline-size: 100%`) and fit inside the page shell grid layout.