# Test Naming Convention Reference

## Version Policy

- Use ALWAYS the latest stable xUnit version available at implementation time.
- Official xUnit website: https://xunit.net/

## Scope

- This guide is generic and reusable across projects.
- Use domain-neutral names unless the repository explicitly requires domain terms.

## Pattern: `[Verb]_[Scenario]_[ExpectedResult]`

Each test name explicitly describes what is being tested, under what conditions, and what should happen.

---

## Components Explained

### [Verb] - The Action

The verb describes the operation being tested:

| Verb | Meaning | Example |
|------|---------|---------|
| `Create` | Creating a new entity | `Create_WithValidData_ReturnsEntity` |
| `Add` | Adding to a collection/database | `Add_WithValidEntity_PersistsToDatabase` |
| `Get` / `Query` | Retrieving data | `Get_WithValidId_ReturnsEntity` |
| `Update` / `Modify` | Changing existing data | `Update_WithNewName_PersistsChanges` |
| `Delete` / `Remove` | Deleting data | `Delete_WithExistingId_RemovesSuccessfully` |
| `Save` | Persisting to storage | `Save_WithValidData_UpdatesDatabase` |
| `Validate` | Checking validation rules | `Validate_WithInvalidEmail_ReturnsFalse` |
| `Parse` / `Convert` | Transforming data | `Parse_WithValidJson_ReturnsEntity` |
| `Search` / `Filter` | Finding with criteria | `Search_WithActiveFilter_ReturnsOnlyActive` |
| `Increment` / `Decrement` | Mathematical operations | `Increment_WithOne_ReturnsTwo` |

### [Scenario] - The Condition

The scenario describes the specific input or context:

| Scenario Pattern | Meaning | Example |
|------------------|---------|---------|
| `With[Property][Value]` | Specific property value | `WithValidData`, `WithNullName`, `WithEmptyString` |
| `When[Condition]` | Specific condition | `WhenUserIsAdmin`, `WhenDatabaseIsEmpty` |
| `For[Entity]` | Specific entity type | `ForArticolo`, `ForUser` |
| `Using[Dependency]` | Specific dependency | `UsingMockedRepository` |
| `Given[State]` | Starting state | `GivenExistingEntity` |

**Examples**:
- `With ValidData` - Entity initialized correctly
- `With NullName` - Name property is null
- `With DuplicateId` - ID already exists
- `With MissingRequiredField` - Required field not initialized
- `With ActiveFilter` - Filter applied for active items only
- `When EmptyCollection` - Collection has no items

### [ExpectedResult] - The Outcome

The result describes what should happen:

| Result Pattern | Meaning | Example |
|----------------|---------|---------|
| `Returns[Type]` | Returns a value | `ReturnsEntity`, `ReturnsTrue` |
| `Persists[Action]` | Saves to storage | `PersistsToDatabase`, `UpdatesRecord` |
| `Throws[Exception]` | Raises an exception | `ThrowsArgumentNullException` |
| `Updates[Property]` | Modifies a value | `UpdatesName`, `ChangesStatus` |
| `Removes[Entity]` | Deletes an item | `RemovesFromDatabase` |
| `Sets[Property]` | Initializes a property | `SetsPrimaryKey` |

**Examples**:
- `ReturnsArticolo` - Returns Articolo entity
- `ThrowsArgumentNullException` - Throws specific exception
- `PersistsToDatabase` - Saves changes successfully
- `ReturnsTrue` / `ReturnsFalse` - Boolean result
- `IncreasesCount` - Collection size increases

---

## Complete Examples

### ✅ GOOD: Clear & Descriptive

```
Create_WithValidData_ReturnsEntity
Create_WithNullName_ThrowsArgumentNullException
Update_WithExistingId_PersistsChanges
Delete_WithValidId_RemovesFromDatabase
Query_WithActiveFilter_ReturnsOnlyActive
Validate_WithEmptyEmail_ReturnsFalse
Parse_WithValidJson_ReturnsCorrectType
Save_WithAuditMetadata_SetsAuthorProperty
Get_WithNonExistentId_ReturnsNull
Add_WithDuplicateItem_ThrowsDuplicateException
```

### ❌ BAD: Vague or Non-Descriptive

| Bad | Problem | Better |
|-----|---------|--------|
| `Test` | Meaningless name | `Create_WithValidData_ReturnsEntity` |
| `TestArticolo` | Vague intent | `Create_WithValidData_ReturnsEntity` |
| `Test1`, `Test2` | No semantic meaning | Descriptive names per pattern |
| `CreateTest` | Verb at wrong end | `Create_WithValidData_ReturnsEntity` |
| `CreateArticolo` | Missing scenario & result | `Create_WithValidData_ReturnsEntity` |
| `Create_Returns` | Missing scenario | `Create_WithValidData_ReturnsEntity` |
| `InvalidInput` | Not a test name | `Validate_WithInvalidInput_ReturnsFalse` |
| `CanCreateArticolo` | Assertion, not action | `Create_WithValidData_ReturnsEntity` |

---

## Naming by Test Type

### Unit Tests

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `Increment_WithPositiveNumber_ReturnsIncrementedValue`
- `Parse_WithValidString_ReturnsObject`
- `ValidateName_WithEmptyString_ReturnsFalse`

### Integration Tests

```
Operation_WithScenario_PersistsCorrectly
```

Examples:
- `Save_WithValidEntity_PersistsToDatabase`
- `Update_WithModifiedProperties_UpdatesSuccessfully`
- `Delete_WithExistingRecord_RemovesFromDatabase`

### Validation Tests (FluentValidation)

```
Validate_WithScenario_ReturnsError
```

Examples:
- `Validate_WithMissingName_ReturnsNameRequiredError`
- `Validate_WithInvalidEmail_ReturnsEmailFormatError`
- `Validate_WithNullDate_ReturnsDateRequiredError`

### Async Tests

Use same pattern, add `Async`:

```
MethodAsync_Scenario_ExpectedResult
MethodName_Scenario_ReturnsAsyncTask
```

Examples:
- `GetAsync_WithValidId_ReturnsArticolo`
- `SaveAsync_WithValidEntity_PersistsToDatabase`
- `QueryAsync_WithFilter_ReturnsMatchingItems`

---

## Quick Checklist

Before naming a test, verify:

- [ ] **Contains Verb**: Action is clear (Create, Update, Delete, etc.)
- [ ] **Contains Scenario**: Specific condition is described (WithValidData, WithNull, etc.)
- [ ] **Contains Expected Result**: Outcome is explicit (ReturnsEntity, ThrowsException, etc.)
- [ ] **No "Test" suffix**: Don't add "Test" at the end
- [ ] **Starts with lowercase verb**: Standard C# naming
- [ ] **Readable aloud**: "Create with valid data returns entity" makes sense
- [ ] **Unique per class**: No duplicate names in test file
- [ ] **Describes one thing**: Not "Create and Save", but separate tests

---

## Real-World Examples

### Articolo Tests

```
Create_WithValidArticoloData_ReturnsArticolo
Create_WithMissingRequiredField_ThrowsException
Update_WithNewName_UpdatesDatabase
Delete_WithExistingId_RemovesSuccessfully
Query_WithStagioneFilter_ReturnsMatchingArticoli
Validate_WithDuplicateSerialNumber_ReturnsFalse
```

### Versione Tests

```
Create_WithValidVersioneData_ReturnsVersione
Increment_WithValidArticolo_IncreasesVersionNumber
GetLatest_WithMultipleVersions_ReturnsNewestVersione
Rollback_WithValidVersionNumber_RestoresArticoloState
```

### Microattivita Tests

```
Create_WithValidMicroattivitaData_ReturnsMicroattivita
AddToLabel_WithExistingLabel_PersistsRelationship
RemoveFromLabel_WithExistingMicroattivita_DeletesLink
Query_WithLabelFilter_ReturnsOnlyMatchingMicroattivita
```

---

## Tools & Validation

To verify naming compliance, look for:
1. **Verb** at the start (Create, Update, Delete, Get, etc.)
2. **Underscore separators** between sections
3. **With/When/For** indicator in scenario
4. **Returns/Persists/Throws** indicator in result
5. **No spaces** in test name (PascalCase for each section)

Example breakdown:
```
Create_WithValidData_ReturnsEntity
├─ Create          (verb - what action)
├─ WithValidData   (scenario - what condition)
└─ ReturnsEntity   (result - what should happen)
```
