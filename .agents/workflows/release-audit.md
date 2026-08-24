---
description: Executes pre-release verification including build, test suite, security secret scan, changelog update, and documentation check.
---

1. Execute solution build (`dotnet build` or `npm run build`) to ensure clean compilation.
2. Execute automated test suite (`dotnet test` or `npm test`).
3. Activate `security-secret-scanner` skill to inspect workspace files and uncommitted diffs for secret leaks.
4. Activate `changelog-reconciler` skill to update `CHANGELOG.md` from git commit history.
5. Verify documentation currency following `documentation-generation.md`.
