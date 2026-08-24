---
trigger: model_decision
description: Angular CLI usage rules for Angular projects
globs: '**/*.component.ts, **/*.ts, **/angular.json'
---

## Component Generation (mandatory command)

Always use `ng generate component` to create components — never create files manually.

### Required Flags

```bash
ng g c features/component-name \
  --inline-template \
  --inline-style \
  --standalone \
  --prefix <app-prefix>
```

| Flag | Required | Reason |
|------|----------|--------|
| `--inline-template` (`-t`) | Yes | Project uses inline templates exclusively |
| `--inline-style` (`-s`) | Yes | Project uses inline styles exclusively |
| `--standalone` | Yes | All components must be standalone |
| `--prefix <app-prefix>` | Yes | Project prefix defined in `angular.json` |
| `--skip-tests` | Only for presentational | Generate tests for components with logic |

### Use `--dry-run` First

Preview generated files before writing:

```bash
ng g c features/my-feature -t -s --standalone --prefix <app-prefix> --dry-run
```

## Other Schematics

```bash
# Service
ng g s services/my-service --skip-tests

# Directive
ng g d directives/my-directive --standalone --skip-tests

# Pipe
ng g p pipes/my-pipe --standalone --skip-tests

# Interface / Class / Enum
ng g interface models/my-model
ng g class models/my-class
ng g enum models/my-enum
```

## Naming Conventions

- Use **kebab-case** for names: `ng g c user-profile`, not `UserProfile`.
- Organize by feature: `ng g c features/auth/login-form`.
- Use `shared/` for reusable components.
- Use `services/`, `models/`, `guards/`, `interceptors/` for respective schematics.

## Project Commands

Run from the Angular project root (check `angular.json` location):

```bash
npm install && npm start    # dev server
npm run build               # production
npm test                    # unit tests (Karma + FirefoxHeadless)
```

- **For large CLI outputs** (warnings, build/test logs): use `headroom_compress` to compress output text to save context space, querying with `headroom_retrieve` if details are needed.

## Anti-Patterns

- MUST NOT create component files manually.
- MUST NOT use separate `.html` or `.css` files.
- MUST NOT omit `--prefix <app-prefix>` if the project uses a non-default prefix (verify in `angular.json`).
- MUST NOT declare components in NgModule in new code.