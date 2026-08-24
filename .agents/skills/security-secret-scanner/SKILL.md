---
name: security-secret-scanner
description: Scans workspace source files, configuration files, and uncommitted git diffs for potential exposed credentials, private keys, or API tokens.
trigger: security-secret-scanner, secret scan, scan secrets, check security, security audit
---

# Security Secret Scanner

## Purpose

Automates pre-commit and on-demand security scanning across project files to prevent accidental leakage of sensitive credentials, API keys, private certificates, or unencrypted database passwords.

## Procedure

1. **Staged and Unstaged Diff Inspection**:
   - Inspect git diffs using `git diff HEAD`.
   - Search for regex patterns indicating credentials:
     - API Tokens: `AKIA[0-9A-Z]{16}`, `ghp_[A-Za-z0-9_]{36}`, `eyJ[A-Za-z0-9_-]+\.`
     - Private Keys: `-----BEGIN RSA PRIVATE KEY-----`, `-----BEGIN PRIVATE KEY-----`
     - Connection Strings: `Server=.*;Database=.*;User Id=.*;Password=.*;`
2. **Environment File Audit**:
   - Check workspace for committed `.env` or `appsettings.Development.local.json` files.
   - Confirm that sensitive config files are ignored in `.gitignore`.
3. **Remediation**:
   - Flag any detected key or secret immediately to the user.
   - Replace plaintext secrets with environment variable placeholders.
