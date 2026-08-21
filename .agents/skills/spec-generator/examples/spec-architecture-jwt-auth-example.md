---
title: JWT Authentication API with Refresh Tokens and Rate Limiting
version: 1.0.0
date_created: 2025-01-16 10:00:00
last_updated: 2025-01-16 10:00:00
owner: Backend Team
tags: [feature, backend, api, security, authentication]
git_commit: abc123def456789
git_branch: feature/jwt-auth
status: draft
related_specs: []
supersedes: []
ai_agent_version: Claude Haiku 4.5
source_purpose: "Implement JWT-based authentication API with refresh tokens, rate limiting, and comprehensive audit logging for the authentication system"
---

# JWT Authentication API with Refresh Tokens and Rate Limiting

This specification defines a production-grade JWT authentication system with refresh tokens, rate limiting (5 attempts per 15 minutes), and audit logging capabilities. The system provides secure user authentication following industry best practices and standards.

## 1. Purpose & Scope

**Purpose**: Implement a secure JWT-based authentication API that supports login, token refresh, logout, and comprehensive audit logging while protecting against brute force attacks through rate limiting.

**Scope**:
- **In Scope**: 
  - User login endpoint with email/password validation
  - JWT access token generation (1-hour expiry)
  - Refresh token mechanism (7-day expiry)
  - Rate limiting (5 failed attempts per 15 minutes per IP)
  - Account lockout after rate limit exceeded
  - Audit logging of all authentication events
  - Logout with token invalidation
  - Password security requirements (bcrypt hashing)
  
- **Out of Scope**:
  - Multi-factor authentication (MFA)
  - Social authentication (OAuth2, SSO)
  - Password reset flows
  - User registration
  - Role-based access control (RBAC) - separate from authentication
  - API key authentication

**Intended Audience**: Backend developers implementing authentication endpoints, DevOps engineers configuring rate limiting, security team reviewing authentication flows

**Assumptions**:
- Users have valid email addresses stored in database
- PostgreSQL is available for user storage
- Azure Key Vault stores JWT signing secrets
- Network infrastructure provides IP tracking
- Base user entity exists with email, password_hash fields

## 2. Definitions & Terminology

| Term | Definition |
|------|------------|
| JWT | JSON Web Token - A compact, URL-safe token format for representing claims between two parties (RFC 7519) |
| Access Token | Short-lived JWT (1 hour) that grants API access; included in request headers |
| Refresh Token | Long-lived JWT (7 days) used to obtain new access tokens; stored securely |
| Token Jti | JWT ID - unique identifier for each token, enables revocation/blacklisting |
| Rate Limiting | Mechanism to limit number of requests from single IP/user (5 failed logins per 15 min) |
| Bcrypt | Password hashing algorithm with salt, security standard for storing passwords |
| Subject (sub) | JWT claim containing user ID |
| Issued At (iat) | JWT claim containing token creation timestamp |
| Expiration (exp) | JWT claim containing token expiration timestamp |
| Claim | Data element in JWT payload (subject, expiry, custom claims) |
| Bearer Token | Authentication method where token is sent in Authorization header as "Bearer <token>" |

## 3. Requirements & Constraints

### 3.1 Functional Requirements

- **REQ-001**: System SHALL accept username/password at POST /api/v1/auth/login endpoint
- **REQ-002**: System MUST validate email format before database lookup (RFC 5322)
- **REQ-003**: System MUST compare incoming password with bcrypt hash using bcrypt.verify()
- **REQ-004**: On successful login, system MUST return HTTP 200 with access token, refresh token, and expiry time
- **REQ-005**: On invalid credentials, system MUST return HTTP 401 Unauthorized without disclosing which field failed
- **REQ-006**: Access tokens MUST expire after 1 hour (3600 seconds)
- **REQ-007**: Refresh tokens MUST expire after 7 days (604800 seconds)
- **REQ-008**: System MUST include JWT ID (jti) claim for token revocation
- **REQ-009**: System MUST include standard claims (sub, iat, exp, jti) in all tokens
- **REQ-010**: System MUST include custom claims (userId, email, role) in access tokens
- **REQ-011**: System MUST validate token signature on every protected request
- **REQ-012**: System MUST support token refresh: POST /api/v1/auth/refresh with valid refresh token returns new access+refresh tokens

### 3.2 Non-Functional Requirements

- **NFR-001**: Performance - Login endpoint MUST respond within 200ms at p95
- **NFR-002**: Availability - Authentication service MUST maintain 99.9% uptime
- **NFR-003**: Scalability - System MUST handle 1,000 concurrent login requests
- **NFR-004**: Latency - Token validation MUST complete in < 50ms (< 10% of request budget)
- **NFR-005**: Database - User lookup MUST use indexed email column (< 5ms response)

### 3.3 Security Requirements

- **SEC-001**: Authentication - All endpoints MUST require valid JWT tokens except /login and /health
- **SEC-002**: Authorization - Users MUST only access resources they own
- **SEC-003**: Data Protection - All sensitive data (passwords, tokens) MUST be encrypted at rest and in transit (TLS 1.3+)
- **SEC-004**: Audit Logging - All authentication events (login success/fail/locked) MUST be logged with timestamp, user ID, IP address, user agent
- **SEC-005**: Password Security - Passwords MUST be hashed with bcrypt (minimum cost factor 12)
- **SEC-006**: Token Storage - Refresh tokens MUST be stored server-side in invalidated_tokens table with expiry
- **SEC-007**: Brute Force Protection - Login MUST fail with 429 after 5 failed attempts within 15 minutes from same IP
- **SEC-008**: Rate Limiting - Failed login counter MUST reset after 15 minutes of inactivity from that IP
- **SEC-009**: Token Signing - JWT MUST be signed with HMAC SHA-256 (HS256) using 256+ bit secret from Azure Key Vault
- **SEC-010**: Token Validation - Token expiration MUST be verified; expired tokens MUST return 401
- **SEC-011**: Logout - Logout endpoint MUST add token to invalidated_tokens table preventing reuse
- **SEC-012**: No Logging - MUST NOT log passwords, tokens, or personally identifiable information in plaintext

**Threat Model**:

| Threat ID | Scenario | Impact | Mitigation | Priority |
|-----------|----------|--------|------------|----------|
| THR-001 | Brute force password guessing | High | Rate limit (5 per 15min), account lockout | Critical |
| THR-002 | Token theft via XSS | High | HttpOnly cookies, CSRF tokens, CSP headers | Critical |
| THR-003 | Token replay attacks | High | Use JWT jti for one-time use, sign with secret | Critical |
| THR-004 | Weak password policy | Medium | Enforce minimum 12 chars, enforce complexity | High |
| THR-005 | Timing attacks on password comparison | Medium | Use bcrypt.verify() (constant-time) | High |
| THR-006 | SQL injection in email validation | High | Use parameterized queries, input validation | Critical |
| THR-007 | Credential stuffing | Medium | Rate limit + CAPTCHA after 3 failures | High |

### 3.4 Compliance Requirements

- **COM-001**: GDPR - System MUST allow users to delete authentication logs within 30 days of request
- **COM-002**: SOC2 - All authentication events MUST be logged and retained for 1 year minimum
- **COM-003**: HIPAA (if applicable) - Passwords MUST be hashed with BCRYPT, not stored in plaintext
- **COM-004**: PCI-DSS - All sensitive authentication data MUST be encrypted at rest and in transit

### 3.5 Constraints

- **CON-001**: Technology - MUST use .NET 8 runtime (existing infrastructure requirement)
- **CON-002**: Database - MUST use PostgreSQL 14+ (existing standard)
- **CON-003**: Timeline - Implementation MUST complete within 2 sprints (2 weeks)
- **CON-004**: Compatibility - MUST not break existing mobile apps (support v1 endpoints)
- **CON-005**: Dependencies - MUST use Microsoft.AspNetCore.Authentication.JwtBearer NuGet package

### 3.6 Guidelines & Best Practices

- **GUD-001**: Use established libraries (Microsoft.IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt) rather than custom token implementation
- **GUD-002**: Follow REST API conventions from `.agents/rules/dotnet-minimal-api.md`
- **GUD-003**: Apply Repository Pattern for data access per architecture standards
- **GUD-004**: Use dependency injection for service access (ITokenService, IUserRepository)
- **GUD-005**: Implement structured logging using ILogger (not Console.WriteLine)

## 4. Architecture & Interfaces

### 4.1 System Architecture

```
┌─────────────────┐
│   Mobile/Web    │
│     Client      │
└────────┬────────┘
         │ POST /api/v1/auth/login (email, password)
         v
┌─────────────────────────────────────┐
│   API Gateway / ASP.NET Core        │
│   (Rate Limiting Middleware)         │
└────────┬────────────────────────────┘
         │
         v
┌─────────────────────────────────────┐
│   Authentication Service (.NET 8)   │
│   ├─ Login Controller                │
│   ├─ JwtTokenService                 │
│   ├─ UserRepository                  │
│   ├─ RateLimitService                │
│   └─ AuditLogger                     │
└────────┬────────────────────────────┘
         │
         v
┌─────────────────────────────────────┐
│   Database (PostgreSQL)             │
│   ├─ users (email, password_hash)   │
│   ├─ invalidated_tokens (jti, exp)  │
│   ├─ audit_logs (events)            │
│   └─ login_attempts (IP, count)     │
└─────────────────────────────────────┘
         │
         v
┌─────────────────────────────────────┐
│   Azure Key Vault                   │
│   └─ JWT Secret (256+ bits)         │
└─────────────────────────────────────┘
```

### 4.2 API Contracts

**Endpoint**: `POST /api/v1/auth/login`

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "deviceId": "uuid-optional"
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLWlkIiwiaWF0IjoxNjc2MzQ1MjAwLCJleHAiOjE2NzYzNDg4MDB9.signature",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLWlkIiwianRpIjoib3JpZyp0b2tlbmlkIiwiZXhwIjoxNjc2OTUwMDAwfQ.signature",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com"
  }
}
```

**Error Responses**:
- `400 Bad Request` - Invalid email format or missing fields
  ```json
  {"error": {"code": "INVALID_REQUEST", "message": "Email format invalid"}}
  ```
- `401 Unauthorized` - Invalid credentials (generic message, don't disclose which field)
  ```json
  {"error": {"code": "INVALID_CREDENTIALS", "message": "Invalid email or password"}}
  ```
- `429 Too Many Requests` - Rate limit exceeded (brute force protection)
  ```json
  {"error": {"code": "TOO_MANY_REQUESTS", "message": "Too many login attempts. Try again in 15 minutes", "retryAfter": 900}}
  ```
- `500 Internal Server Error` - Server error
  ```json
  {"error": {"code": "INTERNAL_ERROR", "message": "An error occurred. Please try again later"}}
  ```

---

**Endpoint**: `POST /api/v1/auth/refresh`

**Request**:
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response** (200 OK): Same as login response

**Error Responses**:
- `401 Unauthorized` - Invalid or expired refresh token
- `400 Bad Request` - Refresh token not provided

---

**Endpoint**: `POST /api/v1/auth/logout`

**Request**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response** (204 No Content): Token invalidated

### 4.3 Data Models

**User Entity**:
```csharp
public class User : Entity<string>
{
    public string Email { get; set; }              // Unique, indexed
    public string PasswordHash { get; set; }       // Bcrypt hash
    public DateTime CreatedAt { get; set; }        // UTC
    public DateTime? LastLoginAt { get; set; }     // UTC, nullable
    public bool IsActive { get; set; } = true;     // Soft delete
}
```

**InvalidatedToken Entity** (for logout/revocation):
```csharp
public class InvalidatedToken : Entity<string>
{
    public string Jti { get; set; }                // JWT ID
    public string UserId { get; set; }             // Foreign key
    public DateTime ExpiresAt { get; set; }        // Token expiry
    public DateTime CreatedAt { get; set; }        // When invalidated
}
```

**LoginAttempt Entity** (for rate limiting):
```csharp
public class LoginAttempt : Entity<string>
{
    public string IpAddress { get; set; }          // Client IP
    public string Email { get; set; }              // Attempted email
    public bool Success { get; set; }              // Success/failure
    public DateTime CreatedAt { get; set; }        // UTC timestamp
}
```

**Database Schema**:
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_active ON users(is_active) WHERE is_active = TRUE;

CREATE TABLE invalidated_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    jti VARCHAR(255) NOT NULL UNIQUE,
    user_id UUID NOT NULL REFERENCES users(id),
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_invalidated_tokens_expires ON invalidated_tokens(expires_at);
CREATE INDEX idx_invalidated_tokens_jti ON invalidated_tokens(jti);

CREATE TABLE login_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ip_address VARCHAR(45) NOT NULL,
    email VARCHAR(255),
    success BOOLEAN NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_login_attempts_ip_time ON login_attempts(ip_address, created_at DESC);
CREATE INDEX idx_login_attempts_email_time ON login_attempts(email, created_at DESC);
```

## 5. Dependencies & External Integrations

### 5.1 Architectural Dependencies

- **ARCH-001**: .NET 8 Runtime
  - **Rationale**: Existing infrastructure standardized on .NET 8; team expertise with .NET ecosystem
  - **Constraint**: MUST use .NET 8 or higher

- **ARCH-002**: Entity Framework Core 8
  - **Rationale**: ORM for database access; migrations; query optimization
  - **Constraint**: Must support PostgreSQL and UUID generation

### 5.2 External System Integrations

- **EXT-001**: Azure Key Vault
  - **Type**: REST API calls
  - **Data Flow**: Outbound (retrieve JWT secret at startup)
  - **SLA Requirements**: 99.9% availability
  - **Authentication**: Managed Identity
  - **Error Handling**: Fail-fast if secret unavailable at startup

### 5.3 Platform & Runtime Requirements

- **PLT-001**: PostgreSQL 14+
  - **Rationale**: Existing database standard; UUID support; partial indexes for rate limiting
  - **Constraint**: Must support gen_random_uuid() and WHERE clauses in indexes

- **PLT-002**: Docker 20.10+
  - **Rationale**: Containerization for AKS deployment
  - **Constraint**: Must support linux/amd64 and linux/arm64 architectures

### 5.4 Third-Party Services

- **SVC-001**: Azure Key Vault
  - **Purpose**: Secure storage of JWT signing secret
  - **Required Capabilities**: Key retrieval, access auditing, automatic rotation
  - **SLA**: 99.9% availability

### 5.5 Implementation Dependencies (Informational)

**Recommended NuGet Packages**:

- **IMP-001**: Authentication
  - `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT validation
  - **Alternatives**: `IdentityServer4`, `Auth0.AspNetCore.Authentication`

- **IMP-002**: Password Hashing
  - `BCrypt.Net-Next` - Bcrypt implementation
  - **Alternatives**: `Argon2id`, `PBKDF2`

- **IMP-003**: JWT
  - `System.IdentityModel.Tokens.Jwt` - JWT creation and validation
  - **Alternative**: `jose-jwt`

- **IMP-004**: Secrets Management
  - `Azure.Identity` - Managed Identity for Key Vault
  - `Azure.Security.KeyVault.Secrets` - Key Vault client

- **IMP-005**: ORM
  - `Microsoft.EntityFrameworkCore` - EF Core
  - `Microsoft.EntityFrameworkCore.Npgsql` - PostgreSQL provider
  - **Alternatives**: `Dapper`, `NHibernate`

## 6. Acceptance Criteria

- **AC-001**: **Given** valid email and password, **When** POST /api/v1/auth/login called, **Then** API returns 200 OK with valid access token (exp = now + 3600s), refresh token (exp = now + 604800s), and user data

- **AC-002**: **Given** invalid password, **When** login attempted, **Then** API returns 401 Unauthorized without disclosing whether email or password failed

- **AC-003**: **Given** 5 failed login attempts within 15 minutes from same IP, **When** 6th login attempt made, **Then** API returns 429 Too Many Requests with 15-minute retry window

- **AC-004**: **Given** valid refresh token, **When** POST /api/v1/auth/refresh called, **Then** API returns 200 with new access and refresh tokens

- **AC-005**: **Given** valid access token, **When** protected endpoint accessed, **Then** token is validated and request proceeds (or returns 401 if invalid/expired)

- **AC-006**: All authentication events logged to audit_logs table with: timestamp (UTC), user_id, ip_address, user_agent, event_type (login_success/login_fail/login_locked/token_refresh/logout), and outcome

- **AC-007**: **Given** logout called with valid token, **When** invalidated_tokens table checked, **Then** token jti is added and prevents further use

- **AC-008**: Access tokens expire after 3600 seconds; expired token access returns 401 Unauthorized with "token expired" message

- **AC-009**: Rate limiter counter resets after 15 minutes of inactivity from IP address

- **AC-010**: Passwords are stored using bcrypt with cost factor ≥12; plaintext passwords never stored or logged

## 7. Test Automation Strategy

### 7.1 Test Levels

- **Unit Tests**: Individual components (password validation, token generation, rate limiting logic)
  - **Coverage Target**: ≥85% code coverage
  - **Framework**: xUnit with Moq for mocking
  - **Examples**: 
    - JwtTokenService generates tokens with correct claims
    - PasswordValidator rejects weak passwords
    - RateLimitService correctly counts failed attempts

- **Integration Tests**: API endpoints with real database
  - **Coverage Target**: All endpoints, all status codes
  - **Framework**: xUnit + TestContainers for isolated PostgreSQL
  - **Examples**:
    - Login flow end-to-end
    - Rate limiting triggers at correct threshold
    - Token refresh works
    - Logout invalidates token

- **End-to-End Tests**: Complete user flows
  - **Coverage Target**: Critical paths (login, token refresh, logout)
  - **Framework**: Playwright or Selenium
  - **Examples**:
    - User logs in, accesses protected resource, logs out
    - Refresh token flow
    - Rate limit protection

### 7.2 Test Data Management

- Use **TestContainers** for isolated PostgreSQL database per test run
- Seed test data via Entity Framework migrations
- Reset database after each test class
- Generate unique emails using `Faker.NET`
- Use bcrypt to hash test passwords

### 7.3 CI/CD Integration

- **Build Pipeline**: Run all tests on every commit
- **PR Requirements**: All tests MUST pass, ≥85% coverage
- **Performance Tests**: Nightly against staging environment
- **Security Scans**: OWASP dependency check, SonarQube analysis

### 7.4 Performance Testing

- **Load Test**: 1,000 concurrent login requests
  - **Tool**: k6 or JMeter
  - **Success Criteria**: p95 < 200ms, 0% error rate

- **Stress Test**: Gradually increase to 5,000 concurrent
  - **Success Criteria**: Identify breaking point, graceful degradation

## 8. Examples & Edge Cases

### 8.1 Successful Login Flow

```csharp
// Arrange
var loginRequest = new LoginRequest 
{ 
    Email = "user@example.com", 
    Password = "SecurePassword123!" 
};

// Act
var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

// Assert
response.StatusCode.Should().Be(HttpStatusCode.OK);
var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
result.AccessToken.Should().NotBeNullOrEmpty();
result.RefreshToken.Should().NotBeNullOrEmpty();
result.ExpiresIn.Should().Be(3600);
result.TokenType.Should().Be("Bearer");
```

### 8.2 Edge Cases

**Case 1: Empty Password**
```
Request: { "email": "user@example.com", "password": "" }
Response: 400 Bad Request - "Password is required"
Status: Email validation prevents empty field submission
```

**Case 2: SQL Injection Attempt**
```
Request: { "email": "user@example.com'; DROP TABLE users; --", "password": "test" }
Response: 400 Bad Request - "Invalid email format"
Status: Email regex validation ([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}) prevents injection
```

**Case 3: Concurrent Login Attempts**
- **Scenario**: User submits 6 login requests simultaneously from same IP
- **Expected Behavior**: First 5 fail with invalid credentials, 6th returns 429 Too Many Requests
- **Validation**: RateLimitService increments counter atomically in database

**Case 4: Token Expiration During Request**
- **Scenario**: Token expires 30ms into request processing
- **Expected Behavior**: Request fails with 401 Unauthorized (no grace period)
- **Rationale**: Short grace period creates security vulnerability; client should refresh proactively

**Case 5: Very Long Password**
- **Scenario**: User submits 10,000 character password
- **Expected Behavior**: Request rejected with 400 Bad Request (max 256 chars)
- **Validation**: Password field has max length constraint

**Case 6: Invalid Email Format**
- **Scenario**: User submits "notanemail" as email
- **Expected Behavior**: Request rejected with 400 Bad Request before database lookup
- **Validation**: Regex validation prevents invalid email format

**Case 7: Nonexistent User**
- **Scenario**: User enters email that doesn't exist
- **Expected Behavior**: Returns 401 Unauthorized (same as wrong password)
- **Rationale**: Don't reveal whether email exists (user enumeration attack prevention)

**Case 8: Rate Limit Reset**
- **Scenario**: User fails 5 times, waits 15 minutes, tries again
- **Expected Behavior**: 6th attempt (after wait) succeeds if credentials valid
- **Validation**: Cleanup job removes old attempts; counter resets

## 9. Validation Criteria

- [ ] All sections of this template are filled out
- [ ] All requirements have unique IDs and explicit MUST/SHALL/SHOULD/MAY language (REQ, NFR, SEC, etc.)
- [ ] All acceptance criteria are testable and measurable
- [ ] All dependencies are documented with rationale
- [ ] API contracts include request/response JSON examples
- [ ] Security requirements include threat model table
- [ ] Task breakdown section is complete with atomic tasks
- [ ] AI-Readiness Checklist passes ≥8/10 items
- [ ] No conflicts with existing specifications
- [ ] Estimated effort totals to 2 sprints (80 hours)

## 10. AI-Readiness Checklist

- [x] **Unambiguous Language**: No idioms, metaphors used; precise terminology (JWT, bcrypt, rate limiting)
- [x] **Complete Definitions**: All acronyms and domain terms defined in section 2
- [x] **Explicit Requirements**: All requirements use MUST/SHALL/SHOULD/MAY keywords (RFC 2119)
- [x] **Testable Criteria**: All acceptance criteria measurable (token expiry=3600s, rate limit=5 per 15min)
- [x] **Self-Contained**: Specification includes all necessary context (no external dependencies)
- [x] **Structured Format**: Proper headings, lists, tables, code blocks throughout
- [x] **Task Granularity**: Section 12 has atomic tasks, each 2-4 hours
- [x] **Dependency Clarity**: Section 5 documents .NET 8, PostgreSQL, Azure Key Vault requirements
- [x] **Error Scenarios**: Section 8.2 covers 8 edge cases with expected behavior
- [x] **Examples Provided**: Section 4.2 has request/response examples; section 8 has code

## 11. Related Specifications & References

### Related Specifications

- [spec-architecture-api-standards.md](../../instructions/dotnet.minimalapi.instructions.md) - REST API conventions
- [spec-design-error-handling.md](../../instructions/dotnet.minimalapi.instructions.md) - Error response formats
- [spec-infrastructure-azure-deployment.md](../../instructions/instructions.instructions.md) - Azure deployment

### External Documentation

- [RFC 7519 - JSON Web Token (JWT)](https://tools.ietf.org/html/rfc7519)
- [RFC 5322 - Internet Message Format (Email validation)](https://tools.ietf.org/html/rfc5322)
- [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [Microsoft Identity Platform Docs](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Bcrypt Security Analysis](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)

## 12. Task Breakdown for Implementation

### Task Format (YAML)

```yaml
tasks:
  - id: TASK-001
    title: "Create User and InvalidatedToken entities with database migration"
    type: code
    priority: critical
    estimated_effort: small
    dependencies: []
    
    objective: |
      Create User entity with Email, PasswordHash, IsActive properties.
      Create InvalidatedToken entity for token revocation.
      Generate EF Core migration to create users and invalidated_tokens tables.
    
    preconditions:
      - PostgreSQL database running
      - Entity Framework Core configured
      - Database connection string available
    
    acceptance_criteria:
      - AC: User entity exists with Id, Email (unique index), PasswordHash, CreatedAt, LastLoginAt, IsActive
      - AC: InvalidatedToken entity exists with Id, Jti (unique), UserId (FK), ExpiresAt, CreatedAt
      - AC: users table created with email index and is_active partial index
      - AC: invalidated_tokens table created with jti and expires_at indexes
      - AC: Migration can be applied successfully (dotnet ef database update)
    
    implementation_hints:
      - Use Fluent API in DbContext.OnModelCreating for configuration
      - Email property requires [EmailAddress] and [Required] annotations
      - Create unique index on email column
      - Use HasIndex() with IsUnique = true
      - Password hash should be 255+ chars to store bcrypt output
    
    files_to_create:
      - path: /src/Domain/Entities/User.cs
        reason: User domain entity model
      - path: /src/Domain/Entities/InvalidatedToken.cs
        reason: Token invalidation tracking
      - path: /src/Infrastructure/Data/Configurations/UserConfiguration.cs
        reason: EF Core entity configuration for User
      - path: /src/Infrastructure/Data/Configurations/InvalidatedTokenConfiguration.cs
        reason: EF Core entity configuration for InvalidatedToken
      - path: /src/Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_CreateAuthTables.cs
        reason: Database migration
    
    validation:
      - Run: dotnet ef migrations list
      - Verify: Migration appears in list
      - Run: dotnet ef database update
      - Verify: No errors during migration
      - Run: psql -c "\d users; \d invalidated_tokens"
      - Verify: Table schemas match specification section 4.3
    
    estimated_completion: 1.5 hours

  - id: TASK-002
    title: "Implement JWT token generation and validation service"
    type: code
    priority: critical
    estimated_effort: medium
    dependencies: [TASK-001]
    
    objective: |
      Create JwtTokenService that generates JWT access tokens (1h expiry) and refresh tokens (7d expiry).
      Implement token validation with signature verification and expiration checks.
      Generate JWT ID (jti) for each token to support revocation.
    
    preconditions:
      - User entity exists (TASK-001 complete)
      - JWT secret available in configuration or Azure Key Vault
      - Microsoft.IdentityModel.Tokens package installed
    
    acceptance_criteria:
      - AC: GenerateAccessToken() returns JWT with sub (user ID), iat, exp (now + 3600s), jti claims
      - AC: GenerateRefreshToken() returns JWT with sub, exp (now + 604800s), jti claims
      - AC: Tokens signed with HS256 using 256+ bit secret from Azure Key Vault
      - AC: ValidateToken() verifies signature and expiration; throws SecurityTokenException if invalid
      - AC: Unit tests cover: valid token generation, expired token rejection, signature mismatch detection
      - AC: 85%+ code coverage for JwtTokenService class
    
    implementation_hints:
      - Use System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
      - Create SecurityTokenDescriptor with claims and expiry
      - Include NewId.NextGuid().ToString() for unique jti claim
      - Use SigningCredentials with HMAC SHA-256 algorithm
      - Implement ITokenValidationParameters with ValidateIssuerSigningKey = true
      - Load secret from IConfiguration or Azure Key Vault at service startup
      - Use dependency injection for IJwtTokenService interface
    
    files_to_create:
      - path: /src/Application/Interfaces/IJwtTokenService.cs
        reason: Service interface for dependency injection
      - path: /src/Infrastructure/Services/JwtTokenService.cs
        reason: JWT token service implementation
      - path: /tests/Unit/Infrastructure/Services/JwtTokenServiceTests.cs
        reason: Unit tests (xUnit) for token generation and validation
    
    validation:
      - Run: dotnet test --filter JwtTokenService --logger "console;verbosity=detailed"
      - Verify: All tests pass
      - Verify: Code coverage ≥85% (use Coverlet or OpenCover)
      - Manual: Decode generated token at jwt.io; verify claims
      - Manual: Modify token signature; verify ValidateToken() throws exception
    
    estimated_completion: 3 hours

  - id: TASK-003
    title: "Implement rate limiting service for login attempts"
    type: code
    priority: critical
    estimated_effort: medium
    dependencies: [TASK-001]
    
    objective: |
      Create RateLimitService tracking failed login attempts per IP address.
      Enforce 5-attempt limit per 15-minute window.
      Lock account for 15 minutes after threshold exceeded.
      Auto-cleanup old attempts after 15 minutes.
    
    preconditions:
      - LoginAttempt entity and database table exist
      - Current timestamp/UTC time available
      - Database connection for reading/writing attempts
    
    acceptance_criteria:
      - AC: CheckRateLimit(ipAddress) allows ≤5 failed attempts per 15 minutes
      - AC: After 5 failures, CheckRateLimit returns locked=true, retryAfter=900 (seconds)
      - AC: Locked IPs can retry after 15 minutes (old attempts expired)
      - AC: RecordAttempt(ipAddress, email, success) adds LoginAttempt record atomically
      - AC: CleanupExpiredAttempts() removes entries older than 15 minutes
      - AC: Unit tests cover: counting, threshold, lockout, cleanup, reset after window
    
    implementation_hints:
      - Query LoginAttempt table with WHERE ip_address = @ip AND created_at > now() - interval '15 minutes' AND success = false
      - Use COUNT(*) to check against limit of 5
      - Store failure count with timestamp for lockout decision
      - Use index on (ip_address, created_at DESC) for query performance
      - Implement CleanupExpiredAttempts as background job (Hangfire or similar)
      - Consider distributed cache (Redis) for high-volume deployments (future optimization)
    
    files_to_create:
      - path: /src/Domain/Entities/LoginAttempt.cs
        reason: Entity for tracking login attempts
      - path: /src/Application/Interfaces/IRateLimitService.cs
        reason: Service interface
      - path: /src/Infrastructure/Services/RateLimitService.cs
        reason: Rate limiting implementation
      - path: /tests/Unit/Infrastructure/Services/RateLimitServiceTests.cs
        reason: Unit tests (xUnit)
    
    validation:
      - Run: dotnet test --filter RateLimitService
      - Verify: All tests pass
      - Scenario: Submit 6 requests from same IP; verify 6th returns 429
      - Scenario: Wait 15 minutes; verify 7th request succeeds
      - Database check: SELECT COUNT(*) FROM login_attempts; verify old entries cleaned
    
    estimated_completion: 3 hours

  - id: TASK-004
    title: "Implement login endpoint with credential validation"
    type: code
    priority: critical
    estimated_effort: medium
    dependencies: [TASK-001, TASK-002, TASK-003]
    
    objective: |
      Create POST /api/v1/auth/login endpoint.
      Validate email format before database lookup.
      Hash password comparison using bcrypt.verify().
      Return JWT tokens on success.
      Enforce rate limiting.
      Log all attempts to audit_logs.
    
    preconditions:
      - JwtTokenService, RateLimitService, UserRepository created
      - AspNetCoreRateLimit or custom rate limiting configured
      - Audit logging framework in place
      - ASP.NET Core controllers configured
    
    acceptance_criteria:
      - AC: Endpoint accepts POST /api/v1/auth/login with email and password in request body
      - AC: Invalid email format returns 400 Bad Request before database lookup
      - AC: Valid email + invalid password returns 401 Unauthorized (no user enumeration)
      - AC: Valid credentials return 200 OK with access_token, refresh_token, expires_in, token_type, user object
      - AC: Failed logins increment rate limit counter per IP
      - AC: After 5 failures per 15min from IP, next attempt returns 429 Too Many Requests
      - AC: All authentication events logged to audit_logs (email, ip_address, user_agent, success/failure)
      - AC: Passwords never logged or exposed in responses
      - AC: Response time < 200ms at p95 (load test verification)
    
    implementation_hints:
      - Use [ApiController] and [Route("api/v1/auth")] attributes
      - Email validation: Regex pattern or EmailAddressAttribute
      - bcrypt password comparison: BCrypt.Net.Verify(plaintext, hash)
      - Inject IUserRepository, IJwtTokenService, IRateLimitService via constructor
      - Use ILogger for audit logging (structured logging)
      - Extract IP from HttpContext.Connection.RemoteIpAddress
      - Return generic 401 for both "user not found" and "wrong password"
      - Test with Postman or RestClient.Test
    
    files_to_create:
      - path: /src/API/Controllers/AuthController.cs
        reason: Login endpoint controller
      - path: /src/API/Models/LoginRequest.cs
        reason: Request DTO with [Required] attributes
      - path: /src/API/Models/LoginResponse.cs
        reason: Response DTO
      - path: /tests/Integration/API/AuthControllerTests.cs
        reason: Integration tests using TestContainers
    
    validation:
      - Run: dotnet test --filter AuthController
      - HTTP POST to /api/v1/auth/login with valid credentials; verify 200 response
      - HTTP POST with invalid password; verify 401 response
      - HTTP POST with invalid email format; verify 400 response
      - Send 6 requests from same IP within 15min; verify 6th returns 429
      - Check audit_logs table; verify all attempts logged
      - Load test: 1,000 concurrent requests; verify p95 < 200ms
    
    estimated_completion: 4 hours

  - id: TASK-005
    title: "Implement token refresh endpoint"
    type: code
    priority: high
    estimated_effort: small
    dependencies: [TASK-002, TASK-004]
    
    objective: |
      Create POST /api/v1/auth/refresh endpoint.
      Accept refresh token from request body.
      Validate refresh token signature and expiry.
      Generate new access and refresh tokens.
      Log refresh event to audit_logs.
    
    preconditions:
      - JwtTokenService validates tokens
      - Audit logging in place
      - AuthController base structure exists
    
    acceptance_criteria:
      - AC: Endpoint accepts POST /api/v1/auth/refresh with refreshToken in body
      - AC: Valid refresh token returns 200 with new access_token and refresh_token
      - AC: Invalid/expired refresh token returns 401 Unauthorized
      - AC: Token refresh logged to audit_logs with success/failure status
      - AC: Unit tests cover: valid token, expired token, malformed token, missing token
    
    implementation_hints:
      - Reuse JwtTokenService.ValidateToken() for refresh token validation
      - Generate new access and refresh tokens with updated exp claims
      - Return same response format as login endpoint
      - Inject IRateLimitService (no rate limiting on refresh, but optional enhancement)
    
    files_to_create:
      - path: /src/API/Models/RefreshTokenRequest.cs
        reason: Request DTO
      - path: /tests/Integration/API/AuthRefreshTests.cs
        reason: Integration tests
    
    validation:
      - Run: dotnet test --filter AuthRefresh
      - HTTP POST with valid refresh token; verify 200 response with new tokens
      - HTTP POST with expired refresh token; verify 401 response
      - Decode new tokens; verify exp claim updated
    
    estimated_completion: 1.5 hours

  - id: TASK-006
    title: "Implement logout endpoint with token invalidation"
    type: code
    priority: high
    estimated_effort: small
    dependencies: [TASK-001, TASK-002, TASK-004]
    
    objective: |
      Create POST /api/v1/auth/logout endpoint.
      Accept access token from request or header.
      Add token jti to invalidated_tokens table.
      Prevent token reuse after logout.
      Log logout event.
    
    preconditions:
      - InvalidatedToken entity and database table exist
      - JWT token contains jti claim
      - Audit logging framework configured
    
    acceptance_criteria:
      - AC: Endpoint accepts POST /api/v1/auth/logout with access token
      - AC: Valid token returns 204 No Content after invalidation
      - AC: Token jti added to invalidated_tokens table
      - AC: Same token rejected on subsequent API calls (401 Unauthorized)
      - AC: Logout event logged to audit_logs
      - AC: Tests cover: valid token, invalid token, already-logged-out token
    
    implementation_hints:
      - Extract jti from JWT token claims
      - Check if jti exists in InvalidatedToken table during token validation
      - Use [Authorize] attribute to require valid token first
      - Add cleanup job to delete expired entries from invalidated_tokens
    
    files_to_create:
      - path: /src/Application/Interfaces/IInvalidatedTokenService.cs
        reason: Token invalidation service interface
      - path: /src/Infrastructure/Services/InvalidatedTokenService.cs
        reason: Implementation
      - path: /tests/Integration/API/AuthLogoutTests.cs
        reason: Integration tests
    
    validation:
      - Run: dotnet test --filter AuthLogout
      - HTTP POST with valid token; verify 204 response
      - Query invalidated_tokens; verify token jti added
      - Use same token on protected endpoint; verify 401 response
    
    estimated_completion: 1.5 hours

  - id: TASK-007
    title: "Implement audit logging for authentication events"
    type: code
    priority: high
    estimated_effort: small
    dependencies: [TASK-004, TASK-005, TASK-006]
    
    objective: |
      Create AuditLog entity for tracking authentication events.
      Log all login attempts (success/failure/locked).
      Log token refresh and logout events.
      Include timestamp, user ID, IP, user agent in logs.
      Implement database migration for audit_logs table.
    
    preconditions:
      - Database migrations framework in place
      - ILogger infrastructure configured
      - Authentication endpoints created
    
    acceptance_criteria:
      - AC: AuditLog entity exists with EventType, UserId, IpAddress, UserAgent, Success, CreatedAt fields
      - AC: Login success/failure/locked events logged with all details
      - AC: Token refresh and logout events logged
      - AC: Audit logs never contain passwords, tokens, or sensitive data
      - AC: Audit table has index on (CreatedAt DESC) for query performance
      - AC: Retention policy: logs kept for 1 year minimum
      - AC: Tests verify audit logging working correctly
    
    implementation_hints:
      - Create AuditLog entity and DbSet in context
      - Create background job (Hangfire) for cleanup of logs older than 1 year
      - Use structured logging with properties: { UserId, IpAddress, EventType, Success, Timestamp }
      - Don't log sensitive fields (email in error messages only)
      - Consider SeriLog for structured logging output to file/centralized logging
    
    files_to_create:
      - path: /src/Domain/Entities/AuditLog.cs
        reason: Audit log entity
      - path: /src/Infrastructure/Data/Configurations/AuditLogConfiguration.cs
        reason: EF configuration
      - path: /src/Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_CreateAuditLogsTable.cs
        reason: Database migration
      - path: /src/Infrastructure/Services/AuditLoggingService.cs
        reason: Audit logging service
    
    validation:
      - Run: dotnet test --filter AuditLogging
      - Perform login; check audit_logs table; verify entry exists
      - Verify logs contain correct event type, user ID, IP, timestamp
      - Verify no passwords or tokens in log records
    
    estimated_completion: 2 hours

  - id: TASK-008
    title: "Create comprehensive unit and integration tests"
    type: test
    priority: high
    estimated_effort: large
    dependencies: [TASK-004, TASK-005, TASK-006]
    
    objective: |
      Create xUnit test suite covering all authentication flows.
      Unit tests for individual services (JwtTokenService, RateLimitService, etc.).
      Integration tests using TestContainers for database testing.
      End-to-end tests for complete login/refresh/logout flows.
      Achieve ≥85% code coverage.
    
    preconditions:
      - All services and endpoints implemented
      - xUnit and Moq installed
      - TestContainers for PostgreSQL configured
    
    acceptance_criteria:
      - AC: Unit test coverage ≥85% for all services
      - AC: Integration tests cover all API endpoints with TestContainers
      - AC: Tests validate: successful login, rate limiting, token refresh, logout, audit logging
      - AC: Edge cases tested: invalid email, empty password, concurrent requests, token expiration
      - AC: Load test passes: 1,000 concurrent requests, p95 < 200ms, 0% error rate
      - AC: All tests run in CI/CD pipeline on every commit
      - AC: Test database cleaned between test runs
    
    implementation_hints:
      - Use xUnit facts [Fact] and theories [Theory] as appropriate
      - Use Moq for mocking dependencies: new Mock<IJwtTokenService>()
      - Use TestContainers.PostgreSql for test database
      - Create base test class with common setup/teardown
      - Use Faker.NET for generating test data
      - Seed minimal test data via migrations
      - Measure coverage with Coverlet: dotnet test /p:CollectCoverage=true
    
    files_to_create:
      - path: /tests/Unit/Infrastructure/Services/JwtTokenServiceTests.cs
        reason: JWT service unit tests
      - path: /tests/Unit/Infrastructure/Services/RateLimitServiceTests.cs
        reason: Rate limiting unit tests
      - path: /tests/Integration/API/AuthControllerTests.cs
        reason: API integration tests
      - path: /tests/Integration/BaseIntegrationTest.cs
        reason: Base class for integration tests with TestContainers setup
      - path: /tests/Performance/AuthLoadTests.cs
        reason: Performance and load tests (k6 or similar)
    
    validation:
      - Run: dotnet test --logger "console;verbosity=detailed"
      - Verify: All tests pass
      - Run: dotnet test /p:CollectCoverage=true /p:CoverageThreshold=85
      - Verify: Code coverage ≥85%
      - Load test: k6 run tests/performance/auth-load-test.js
      - Verify: 1,000 concurrent users, p95 < 200ms, 0% errors
    
    estimated_completion: 8 hours

  - id: TASK-009
    title: "Documentation and API documentation (Swagger)"
    type: documentation
    priority: medium
    estimated_effort: small
    dependencies: [TASK-004, TASK-005, TASK-006]
    
    objective: |
      Create Swagger/OpenAPI documentation for authentication endpoints.
      Document request/response schemas, error codes, and examples.
      Create developer guide for using JWT tokens in client applications.
      Document setup and configuration instructions.
    
    preconditions:
      - Swagger (Swashbuckle) or similar installed in project
      - All endpoints implemented and tested
    
    acceptance_criteria:
      - AC: Swagger UI accessible at /swagger endpoint
      - AC: All three endpoints documented: /login, /refresh, /logout
      - AC: Request and response examples shown
      - AC: Error codes and descriptions documented (400, 401, 429, 500)
      - AC: Authentication method (Bearer token) documented
      - AC: Developer guide created with sample client code (curl, C#, JavaScript)
      - AC: Configuration instructions for JWT secret in Azure Key Vault documented
    
    implementation_hints:
      - Use Swashbuckle.AspNetCore for automatic Swagger generation
      - Add [ProducesResponseType] attributes to controller actions
      - Add [SwaggerResponse] attributes for detailed responses
      - Create README with setup steps in docs/ directory
    
    files_to_create:
      - path: /docs/api/authentication-api.md
        reason: API documentation
      - path: /docs/guides/jwt-client-implementation.md
        reason: Developer guide for using JWT in clients
    
    validation:
      - Build and run project
      - Navigate to /swagger
      - Verify all endpoints listed and documented
      - Verify example requests/responses displayed correctly
    
    estimated_completion: 2 hours

  - id: TASK-010
    title: "Security testing and vulnerability assessment"
    type: test
    priority: high
    estimated_effort: medium
    dependencies: [TASK-004, TASK-005, TASK-006, TASK-008]
    
    objective: |
      Perform security testing to identify vulnerabilities.
      Test for: SQL injection, brute force, timing attacks, token replay.
      Verify password hashing with bcrypt (cost ≥12).
      Verify no sensitive data leakage in logs or responses.
      Run OWASP dependency check.
    
    preconditions:
      - All endpoints implemented and tested
      - Security requirements defined in section 3.3
      - Testing frameworks available
    
    acceptance_criteria:
      - AC: SQL injection test: malicious email input rejected with 400
      - AC: Brute force test: 6 failed attempts from same IP return 429
      - AC: Timing attack test: password comparison takes consistent time (use bcrypt)
      - AC: Token replay test: invalidated token rejected with 401
      - AC: Sensitive data test: no passwords/tokens in logs or error responses
      - AC: Bcrypt test: verify password hashes have cost ≥12
      - AC: OWASP dependency check passes (no critical vulnerabilities)
      - AC: All issues from security scan remediated or documented as acceptable risk
    
    implementation_hints:
      - Use OWASP ZAP or Burp Community for penetration testing
      - Run: dotnet list package --vulnerable
      - Check bcrypt hash format: $2b$12$ means cost=12
      - Review logs to ensure no sensitive data logged
      - Test timing with: time curl requests with valid/invalid passwords
    
    files_to_create:
      - path: /tests/Security/SecurityTests.cs
        reason: Security test cases
      - path: /docs/security/security-testing-report.md
        reason: Security assessment findings
    
    validation:
      - Run: dotnet list package --vulnerable
      - No critical vulnerabilities
      - Execute security tests: dotnet test --filter Security
      - Review log files for sensitive data
      - Bcrypt verification: Verify cost in database password hashes
      - Create summary report of findings
    
    estimated_completion: 4 hours
```

### Task Progress Tracking

| Task ID | Title | Type | Priority | Status | Assignee | Last Updated |
|---------|-------|------|----------|--------|----------|--------------|
| TASK-001 | Create User entity | code | critical | ❌ Not Started | - | - |
| TASK-002 | JWT token service | code | critical | ❌ Not Started | - | - |
| TASK-003 | Rate limiting service | code | critical | ❌ Not Started | - | - |
| TASK-004 | Login endpoint | code | critical | ❌ Not Started | - | - |
| TASK-005 | Token refresh endpoint | code | high | ❌ Not Started | - | - |
| TASK-006 | Logout endpoint | code | high | ❌ Not Started | - | - |
| TASK-007 | Audit logging | code | high | ❌ Not Started | - | - |
| TASK-008 | Unit/integration tests | test | high | ❌ Not Started | - | - |
| TASK-009 | Documentation | documentation | medium | ❌ Not Started | - | - |
| TASK-010 | Security testing | test | high | ❌ Not Started | - | - |

**Legend**: ✅ Completed | 🔄 In Progress | ⏸️ Blocked | ❌ Not Started

## 13. Conflict Detection & Resolution

### Conflict Analysis

| Conflict ID | Conflicting Spec | Conflict Description | Resolution Strategy |
|-------------|------------------|---------------------|---------------------|
| CNF-001 | (None identified) | No existing auth spec found in /specs/ | Proceed with creation - no conflicts detected |

**Resolution Notes**:
- Searched `/specs/` and `/specs/implemented/` directories
- No existing JWT authentication specifications found
- No conflicting requirements with existing project standards
- This is the initial authentication specification for the system

## 14. Files Added to Context

**Instruction Files Referenced**:
- `.agents/rules/create.specification.instructions.md` - Template and specification rules
- `.agents/rules/csharp-coding-standards.md` - C# coding standards for implementation
- `.agents/rules/dotnet-minimal-api.md` - REST API conventions for ASP.NET Core
- `.agents/rules/dotnet-logging.md` - Logging practices
- `.agents/rules/xunit-testing.md` - Testing standards

**Project Context Files**:
- `.agents/AGENTS.md` - Project overview and architecture context
- `<SolutionFolder>/<SolutionName>.Dal/Models/Entity.cs` - Base entity pattern reference
- `<SolutionFolder>/<SolutionName>.Dal/<SolutionName>Context.cs` - EF Core context pattern reference
- `<SolutionFolder>/<SolutionName>.Dal/Models/User.cs` - User entity as reference
- `<SolutionFolder>/<SolutionName>.Dal/Migrations/` - Migration pattern reference

## 15. Always Follow Project Instructions

This specification adheres to the following project-wide instructions:

### From `.agents/rules/csharp-coding-standards.md`
- Use C# 11+ nullable reference types: `string?`, `bool`, not nullable by default
- Use `required` keyword for mandatory properties
- Use `record` for DTOs and immutable data transfer objects
- Use `init` properties for read-only initialization
- Name classes with clear intent: UserRepository, JwtTokenService, not Helpers or Utils

### From `.agents/rules/dotnet-minimal-api.md`
- Use REST conventions: POST for creation/actions, GET for retrieval, PUT/PATCH for updates, DELETE for removal
- Return appropriate HTTP status codes: 200 (OK), 201 (Created), 400 (Bad Request), 401 (Unauthorized), 429 (Too Many Requests), 500 (Error)
- Use consistent error response format: `{ "error": { "code": "ERROR_CODE", "message": "User-friendly message" } }`
- Version APIs using URL path: `/api/v1/...`
- Use dependency injection for all services

### From `.agents/rules/xunit-testing.md`
- Use xUnit [Fact] and [Theory] attributes for tests
- Follow AAA pattern: Arrange-Act-Assert
- Name tests: `MethodName_StateUnderTest_ExpectedBehavior`
- Achieve ≥80% code coverage for new code
- Use Moq for mocking dependencies

### From `.agents/rules/dotnet-logging.md`
- Use ILogger for structured logging, never Console.WriteLine
- Log authentication events with properties: { UserId, IpAddress, EventType, Success }
- Never log passwords, tokens, or sensitive PII
- Use log levels: LogInformation (normal flow), LogWarning (issues), LogError (failures)

---

## Migration from v1 to v2 (if applicable)

No v1 exists - this is initial implementation.

## Sign-Off

**Created**: 2025-01-16 10:00:00 UTC
**Status**: Draft (ready for review and approval)
**Next Steps**: 
1. Security team review (threat model assessment)
2. Architecture review (alignment with system design)
3. Move to "approved" status
4. Implementation team begins TASK-001
