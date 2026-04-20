# Error Handling Fix Plan — Linq2GraphQL.Client

## Overview
Fix 6 GraphQL error handling deficiencies. 4 phases, 12 steps. Each step leaves codebase buildable and backward-compatible.

## Phase 1: Foundation (Issues 1, 6, 3) — Data model changes only

### Step 1.1: Add `Extensions` property to `GraphQueryError`
- **File**: `src/Linq2GraphQL.Client/Exceptions/GraphQueryExecutionException.cs`
- **Action**: Add `[JsonPropertyName("extensions")] public Dictionary<string, object>? Extensions { get; set; }` to `GraphQueryError`
- **Effort**: 2 min | **Risk**: Low | **BC**: 100%

### Step 1.2: Create `GraphErrorCode` enum + classifier
- **New File**: `src/Linq2GraphQL.Client/Exceptions/GraphErrorCode.cs`
- **Action**: Enum with standard codes (UNAUTHENTICATED, FORBIDDEN, VALIDATION, etc.) + `GraphErrorCodeClassifier` static helper
- **Also**: Add `ErrorCode` computed property to `GraphQueryError`
- **Effort**: 10 min | **Risk**: Low | **BC**: 100%

### Step 1.3: Create `GraphResult<T>` wrapper
- **New File**: `src/Linq2GraphQL.Client/GraphResult.cs`
- **Action**: `GraphResult<T>` with `Data`, `Errors`, `Extensions`, `HasErrors`, `HasData`
- **Effort**: 5 min | **Risk**: Low | **BC**: 100%

## Phase 2: Core Query/Mutation Error Handling (Issues 2, 3)

### Step 2.1: Refactor `QueryExecutor` — internal `ProcessResponseFull`
- **File**: `src/Linq2GraphQL.Client/QueryExecutor.cs`
- **Action**: New `internal GraphResult<T> ProcessResponseFull(...)` that parses data+errors+extensions. Keep existing `ProcessResponse` as backward-compat wrapper.
- **Effort**: 15 min | **Risk**: Medium | **BC**: 100%

### Step 2.2: Add `ExecuteWithResultAsync` to `GraphQueryExecute`
- **File**: `src/Linq2GraphQL.Client/GraphQueryExecute.cs` + `QueryExecutor.cs`
- **Action**: Add `ExecuteRawAsync` to QueryExecutor, add `ExecuteBaseWithResultAsync` + `ExecuteWithResultAsync` to GraphQueryExecute
- **Effort**: 15 min | **Risk**: Medium | **BC**: 100%

### Step 2.3: Add `ExecuteWithResultAsync` to cursor paging
- **File**: `src/Linq2GraphQL.Client/GraphQueryExecute.cs` + `GraphCursorPager.cs`
- **Action**: Same pattern for `GraphCursorQueryExecute` and pager
- **Effort**: 10 min | **Risk**: Low | **BC**: 100%

## Phase 3: Subscription Error Handling (Issues 4, 5)

### Step 3.1: Handle WS `type: "error"` and `type: "complete"` messages
- **Files**: `WebsocketRequestTypes.cs`, `WebsocketResponse.cs`, `WSClient.cs`
- **Action**: Add message type constants, handle error/complete in WSClient message routing, propagate via OnError/OnCompleted
- **Effort**: 25 min | **Risk**: Medium | **BC**: Behavioral fix

### Step 3.2: Add error resilience to subscription pipeline
- **File**: `GraphSubscriptionExecute.cs`
- **Action**: Replace `Select` with `SelectMany` + try/catch — skip bad messages, don't kill stream
- **Effort**: 15 min | **Risk**: Medium | **BC**: Behavioral fix

### Step 3.3: SSE client error handling
- **File**: `SSEClient.cs`
- **Action**: Wrap HTTP errors in `GraphQueryRequestException`, null checks, parse `event: error` SSE frames
- **Effort**: 10 min | **Risk**: Low | **BC**: 100%

## Phase 4: Testing & Verification

### Step 4.1: Add error handling unit tests
- **New File**: `test/Linq2GraphQL.Tests/ErrorHandlingTests.cs`
- **Action**: Test extensions deserialization, GraphResult partial data, error codes, subscription error resilience
- **Effort**: 20 min | **Risk**: Low

### Step 4.2: Run full test suite
- **Action**: `dotnet build` + `dotnet test`
- **Effort**: 5 min

## Dependency Graph
```
1.1 → 1.2 → 1.3 → 2.1 → 2.2 → 2.3
                  ↘
1.1 → 3.1 → 3.2
       ↘ 3.3
→ 4.1 → 4.2
```

## Risk Summary
| Step | Risk | Mitigation |
|------|------|------------|
| 1.1-1.3 | Low | Additive only, no behavioral change |
| 2.1-2.3 | Medium | Keep old APIs, add new opt-in APIs |
| 3.1-3.2 | Medium | Test with both WS protocols |
| 3.3 | Low | Defensive coding |
| 4.1-4.2 | Low | Tests are additive |
