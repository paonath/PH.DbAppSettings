---
title: Order Status Deadlock Fix - Concurrent Update Issue
version: 1.0.0
date_created: 2025-01-16 10:30:00
last_updated: 2025-01-16 10:30:00
owner: Database Team
tags: [bugfix, backend, database, performance, deadlock]
git_commit: def456abc123
git_branch: bugfix/order-deadlock
status: draft
related_specs: []
supersedes: []
ai_agent_version: Claude Haiku 4.5
source_purpose: "Resolve deadlock in sp_UpdateOrderStatus stored procedure when executed concurrently from multiple sessions; implement row-level locking strategy and transaction isolation"
---

# Order Status Deadlock Fix - Concurrent Update Issue

This specification addresses a production deadlock issue in the `sp_UpdateOrderStatus` stored procedure when called concurrently from multiple sessions. The root cause is identified, and a solution using proper row-level locking and transaction isolation is specified.

## 1. Purpose & Scope

**Purpose**: Fix deadlock occurring in `sp_UpdateOrderStatus` stored procedure during concurrent order status updates; implement optimized locking strategy to prevent future occurrences.

**Scope**:
- **In Scope**: 
  - Root cause analysis of current deadlock
  - Redesign sp_UpdateOrderStatus procedure with proper locking (ROWLOCK, UPDLOCK hints)
  - Implement transaction isolation level (READ_COMMITTED_SNAPSHOT)
  - Add retry logic to calling code
  - Create migration script
  - Performance testing to verify no regression

- **Out of Scope**:
  - Full rewrite of order processing system
  - Switching to NoSQL database
  - Horizontal sharding of orders table

**Intended Audience**: Database engineers, backend developers maintaining order processing

**Assumptions**:
- SQL Server 2019+ running
- Orders table has primary key (OrderId)
- Current isolation level is READ_COMMITTED
- Transaction log space available
- Downtime window available for procedure replacement

## 2. Definitions & Terminology

| Term | Definition |
|------|------------|
| Deadlock | Two or more transactions hold resources needed by each other, causing indefinite wait |
| ROWLOCK | SQL Server hint forcing row-level locks instead of page locks |
| UPDLOCK | Update lock acquired for reading; prevents other updates; escalates to exclusive lock |
| Isolation Level | Degree of concurrency control and consistency guarantees in transactions |
| Transaction Log | Write-ahead log tracking all database changes for recovery |
| Page Lock | Lock at 8KB page level (may include multiple rows); can escalate |

## 3. Requirements & Constraints

### 3.1 Functional Requirements

- **REQ-001**: Procedure MUST update order status atomically without deadlock risk
- **REQ-002**: Procedure MUST handle concurrent calls from 10+ sessions without blocking
- **REQ-003**: Procedure MUST validate order exists before update
- **REQ-004**: Procedure MUST prevent invalid status transitions

### 3.2 Non-Functional Requirements

- **NFR-001**: Procedure execution time MUST be < 100ms at p95
- **NFR-002**: Deadlock occurrences MUST drop to zero after fix
- **NFR-003**: Concurrency - System MUST handle 100+ concurrent updates without timeout

### 3.3 Security Requirements

- **SEC-001**: Only authorized users MUST execute the procedure
- **SEC-002**: Update operations MUST be audited

### 3.5 Constraints

- **CON-001**: No breaking API changes - signature must remain identical
- **CON-002**: Rollback must be possible within 30 minutes
- **CON-003**: Must work with existing .NET Entity Framework calls

## 4. Architecture & Interfaces

### 4.1 Current Problem

```sql
-- Current procedure (PROBLEMATIC - causes deadlock)
ALTER PROCEDURE sp_UpdateOrderStatus
  @OrderId BIGINT,
  @NewStatus NVARCHAR(50)
AS
BEGIN
  BEGIN TRANSACTION
    UPDATE Orders
    SET Status = @NewStatus
    WHERE OrderId = @OrderId
    
    -- Deadlock often occurs here when other session
    -- tries to read/update same or related rows
    UPDATE OrderHistory
    SET LastUpdated = GETUTCDATE()
    WHERE OrderId = @OrderId
  COMMIT TRANSACTION
END
```

**Root Cause**: 
- Lock escalation from row to page level
- Two transactions competing for page locks in different order (circular wait)
- Transaction log contention

### 4.2 Fixed Procedure (Solution)

```sql
-- Fixed procedure with proper locking and isolation
ALTER PROCEDURE sp_UpdateOrderStatus
  @OrderId BIGINT,
  @NewStatus NVARCHAR(50)
AS
BEGIN
  SET TRANSACTION ISOLATION LEVEL READ_COMMITTED_SNAPSHOT;
  
  BEGIN TRANSACTION
    -- Use ROWLOCK and UPDLOCK to prevent escalation
    UPDATE Orders WITH (ROWLOCK, UPDLOCK)
    SET Status = @NewStatus
    WHERE OrderId = @OrderId
    
    -- Row is already locked, no deadlock risk here
    UPDATE OrderHistory WITH (ROWLOCK)
    SET LastUpdated = GETUTCDATE()
    WHERE OrderId = @OrderId
  COMMIT TRANSACTION
END
```

## 5. Dependencies & External Integrations

- **ARCH-001**: SQL Server 2019+ (must support READ_COMMITTED_SNAPSHOT)
- **EXT-001**: Existing .NET 8 calling code (no changes needed to API)

## 6. Acceptance Criteria

- **AC-001**: Procedure executes successfully without timeout
- **AC-002**: Concurrent execution (10+ sessions) shows zero deadlock errors
- **AC-003**: Execution time remains < 100ms at p95
- **AC-004**: Existing .NET calls work without modification
- **AC-005**: Rollback script successfully reverts to previous procedure

## 7. Test Automation Strategy

### 7.1 Test Levels

- **Unit Tests**: Procedure logic validation
- **Integration Tests**: .NET → SQL Server execution
- **Concurrency Tests**: 10+ concurrent sessions updating same orders
- **Performance Tests**: 1,000 concurrent updates, measure deadlock occurrences

### 7.3 CI/CD Integration

- Procedure deployed via database migration
- Concurrency tests run on every merge to main
- Performance baselines tracked in CI

## 8. Examples & Edge Cases

**Case 1: Concurrent Updates to Same Order**
- 5 sessions attempt to update same order simultaneously
- Expected: All succeed; no deadlock
- Validation: Row-lock held for each update; queued if needed

**Case 2: Transaction Timeout**
- Session locks row for 10 seconds
- Expected: Other sessions wait (not deadlock)
- Validation: Query timeout > lock wait time

## 13. Conflict Detection & Resolution

No conflicts with existing specifications identified.

## 12. Task Breakdown

```yaml
tasks:
  - id: TASK-001
    title: "Analyze and document deadlock trace"
    type: documentation
    priority: critical
    estimated_effort: small
    objective: Capture SQL Server deadlock graph and document root cause
    validation:
      - Deadlock trace collected from SQL Server Extended Events
      - Root cause documented
```

---

**Status**: Draft - ready for DBA review
