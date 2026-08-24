---
trigger: model_decision
description: Security and secret handling rules prohibiting committed credentials, tokens, and private keys
globs: '**/*'
---

## Core Security Directives

- **MUST NOT** commit plaintext passwords, API keys, JWT secret keys, access tokens, or private RSA keys to the repository.
- **MUST NOT** commit active environment files containing sensitive credentials (such as `.env` or `appsettings.Development.local.json`).
- Use `.env.example` or `appsettings.Example.json` templates with dummy values for configuration placeholders.
- Always use environment variables or secure secret managers for sensitive credentials.

## Pre-Commit Verification

- Review git diffs before committing using `git diff --staged`.
- Verify that no secret patterns (e.g., `AKIA...`, `ghp_...`, `-----BEGIN PRIVATE KEY-----`) are present in uncommitted changes.
- Replace any detected secret literal with a configurable environment placeholder immediately.
