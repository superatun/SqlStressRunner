# SQL Stress Runner

A WPF desktop application for load and stress testing SQL Server stored procedures and raw SQL commands.

## Features

- WPF desktop UI built with MVVM
- Flexible SQL Server connectivity using Windows Authentication or SQL Authentication
- Configurable concurrent execution
- Dynamic support for multiple execution steps per iteration
- Parameter mapping from an initial dataset into each configured command
- Support for both stored procedures and raw SQL text commands
- Detailed metrics including TPS, latency, P95, and P99
- Optional database logging with per-iteration and per-command detail
- Automatic database setup and schema migration from the Connection tab
- Local configuration persistence

## Requirements

- .NET 10 SDK
- SQL Server compatible with `Microsoft.Data.SqlClient`
- Windows 10 or Windows 11

## Getting Started

1. Clone the repository or extract the source code.
2. Open the project in Visual Studio 2022 or newer.
3. Restore NuGet packages.
4. Build the project.
5. Run the application.

## How It Works

Each test run generates a single `RunId`.

For each iteration:

1. A row is selected from the initial dataset.
2. The configured commands are executed in order.
3. Each command execution is recorded independently.
4. If one command fails, the current iteration is marked as failed and the remaining commands in that iteration are skipped.
5. The iteration receives its own `IterationId`.

This gives you a hierarchy like:

- `RunId` -> one full stress test run
- `IterationId` -> one iteration inside that run
- command execution rows -> one row per stored procedure or SQL command executed inside that iteration

## Application Workflow

### 1. Configure the Connection

In the **Connection** tab:

- Enter the SQL Server connection details.
- Choose the authentication mode.
- Use **Test Connection** to validate connectivity.
- Use **Setup Database** to create or migrate the logging schema in the selected database.
- Use **Save Settings** to persist the connection settings locally.

`Setup Database` is idempotent. It can be executed multiple times safely and will:

- create the required logging objects if they do not exist
- migrate supported legacy logging structures
- keep the active schema aligned with the current version of the application

### 2. Configure the Test

In the **Test Configuration** tab:

- **Initial Query**: a query that returns the dataset used to feed parameters into your commands

Example:

```sql
SELECT UserId, ProductId, Quantity
FROM TestData;
```

- Add one or more execution steps.
- Each step can be:
  - a stored procedure call
  - a raw SQL text command

Stored procedure example:

```sql
EXEC CreateOrder @UserId, @ProductId, @Quantity
```

Raw SQL example:

```sql
UPDATE Orders
SET LastTouchedAt = SYSUTCDATETIME()
WHERE UserId = @UserId;

SELECT @@ROWCOUNT AS RowsAffected;
```

Configure:

- `Command Timeout`
- `Max Degree of Parallelism`
- `Total Iterations`
- `Recycle Dataset`
- `Log to Database`
- `Log Table Name`

`Log Table Name` is treated as the base name for the logging schema, not as the name of a single table.

### 3. Load the Initial Dataset

Still in **Test Configuration**, use **Load Initial Data** to execute the initial query and cache the dataset in memory.

### 4. Configure Parameter Mapping

In the **Parameter Mapping** tab:

- Map each column from the initial dataset to the parameters needed by each configured command.
- Mappings are dynamic and tied to the configured command names.

Example:

- `UserId` -> `@UserId` for `CreateOrder`
- `ProductId` -> `@ProductId` for `CreateOrder`
- `OrderId` -> `@OrderId` for `ProcessOrder`

### 5. Run the Test

Back in **Test Configuration**:

- Click **Start Test**
- Monitor execution progress in real time
- Use **Cancel Test** to stop the run if needed

### 6. Review Results

In the **Results** tab you can review:

- the generated `RunId`
- total duration
- total, successful, and failed iterations
- TPS per iteration
- TPS per command
- average, minimum, maximum, P95, and P99 latency
- average duration per configured command

## Logging Schema

When `Log to Database` is enabled, the application writes to a normalized schema.

If `Log Table Name` is set to `StressTestLog`, the active objects are:

- `StressTestIterationLog`
- `StressTestStoredProcedureLog`
- `StressTestLogSummary`
- `vw_StressTestSummary`

Legacy objects may also appear during migration:

- `StressTestLogLegacy`
- `StressTestLogSummaryLegacy`

### `StressTestIterationLog`

Stores one row per iteration:

- `IterationId`
- `RunId`
- `IterationNumber`
- `Success`
- `TotalDurationMs`
- `ExecutedAt`
- `ErrorMessage`

### `StressTestStoredProcedureLog`

Stores one row per executed command within an iteration:

- `IterationId`
- `RunId`
- `IterationNumber`
- `StoredProcedureName`
- `StoredProcedureOrder`
- `Success`
- `ExecutionDurationMs`
- `Parameters`
- `ResponsePayload`
- `ResponseRowCount`
- `ResponseResultSetCount`
- `ErrorMessage`
- `ExecutedAt`

Despite the column name `StoredProcedureName`, this table is used for both stored procedures and raw SQL commands configured in the UI.

### `StressTestLogSummary`

Stores one row per test run:

- `RunId`
- `TotalIterations`
- `SuccessfulIterations`
- `FailedIterations`
- `TotalDurationMs`
- `TpsPerIteration`
- `TpsPerStoredProcedure`
- `AverageLatencyMs`
- `MinLatencyMs`
- `MaxLatencyMs`
- `P95LatencyMs`
- `P99LatencyMs`
- `StartTime`
- `EndTime`

## Query Examples

### Get all iterations for a run

```sql
SELECT *
FROM StressTestIterationLog
WHERE RunId = 'YOUR-RUN-ID'
ORDER BY IterationNumber;
```

### Get every command execution for a run

```sql
SELECT
    RunId,
    IterationId,
    IterationNumber,
    StoredProcedureOrder,
    StoredProcedureName,
    Success,
    ExecutionDurationMs,
    ResponseRowCount,
    ResponseResultSetCount,
    Parameters,
    ResponsePayload,
    ErrorMessage,
    ExecutedAt
FROM StressTestStoredProcedureLog
WHERE RunId = 'YOUR-RUN-ID'
ORDER BY IterationNumber, StoredProcedureOrder;
```

### Get the command rows for one iteration

```sql
SELECT *
FROM StressTestStoredProcedureLog
WHERE IterationId = 'YOUR-ITERATION-ID'
ORDER BY StoredProcedureOrder;
```

### Get only failed command executions for a run

```sql
SELECT
    RunId,
    IterationNumber,
    StoredProcedureOrder,
    StoredProcedureName,
    ErrorMessage,
    Parameters,
    ResponsePayload
FROM StressTestStoredProcedureLog
WHERE RunId = 'YOUR-RUN-ID'
  AND Success = 0
ORDER BY IterationNumber, StoredProcedureOrder;
```

### Get average duration per command for a run

```sql
SELECT
    StoredProcedureName,
    AVG(ExecutionDurationMs) AS AvgDurationMs
FROM StressTestStoredProcedureLog
WHERE RunId = 'YOUR-RUN-ID'
GROUP BY StoredProcedureName
ORDER BY StoredProcedureName;
```

## Metrics Definitions

- **TPS per Iteration**: successful iterations divided by total execution time in seconds
- **TPS per Stored Procedure**: successful command executions per second, based on the number of configured commands
- **Latency**: total iteration execution time
- **P95**: 95 percent of successful iterations completed in this time or less
- **P99**: 99 percent of successful iterations completed in this time or less

## Example Scenarios

### Example 1: Stored procedures only

```sql
-- Initial Query
SELECT TOP 1000 CustomerId, ProductId
FROM Orders;

-- Command 1
EXEC ValidateCustomer @CustomerId;

-- Command 2
EXEC CheckProductStock @ProductId;
```

### Example 2: Mixed raw SQL and stored procedure flow

```sql
-- Initial Query
SELECT TOP 1000 OrderId, CustomerId, Amount
FROM PendingOrders;

-- Command 1
UPDATE Orders
SET LastTouchedAt = SYSUTCDATETIME()
WHERE OrderId = @OrderId;

SELECT @@ROWCOUNT AS RowsAffected;

-- Command 2
EXEC ProcessPayment @OrderId, @Amount;
```

### Example 3: Generated test data

```sql
-- Initial Query
SELECT NEWID() AS OrderId, CustomerId, ProductId, RAND() * 100 AS Amount
FROM Customers
CROSS JOIN Products;

-- Command 1
EXEC CreateOrder @OrderId, @CustomerId, @ProductId, @Amount;

-- Command 2
EXEC ProcessPayment @OrderId, @Amount;
```

## Configuration Persistence

The application stores the last-used configuration in:

```text
%LocalAppData%\SqlStressRunner\config.json
```

For security reasons, the password is not persisted.

## Project Structure

```text
SqlStressRunner/
|-- Models/
|-- ViewModels/
|-- Views/
|-- Services/
|-- Infrastructure/
|-- Commands/
|-- Helpers/
|-- DatabaseSchema.sql
|-- SqlStressRunner.csproj
|-- SqlStressRunner.slnx
```

## Troubleshooting

### Connection errors

- Verify that SQL Server is running.
- Check firewall and network access.
- Confirm credentials and database name.
- If needed, enable `Trust Server Certificate` for self-signed environments.

### Database setup issues

- Make sure the configured login has permission to create tables, indexes, views, and constraints.
- Run **Setup Database** from the **Connection** tab before starting a logged test run.
- Re-run **Setup Database** if you upgraded the app and need schema migration.

### Execution failures during a test

- Individual command failures do not terminate the entire run.
- A failed command stops only the remaining commands in the current iteration.
- Check `StressTestStoredProcedureLog.ErrorMessage` and `Parameters` for the failing row.
- Validate parameter mappings and SQL syntax.

### Slow performance

- Reduce `Max Degree of Parallelism` if the target database is under heavy load.
- Increase `Command Timeout` for long-running operations.
- Review the command-level detail in `StressTestStoredProcedureLog` to locate bottlenecks.

## Technology Stack

- .NET 10
- WPF
- MVVM
- ADO.NET via `Microsoft.Data.SqlClient`
- `System.Text.Json`

## Architecture Notes

- MVVM separation between UI and application logic
- Async execution to keep the UI responsive
- Per-worker SQL connections
- Parallel iteration processing using `Parallel.ForEachAsync`
- Thread-safe in-memory result collection before summary calculation

## License

Free to use for personal and commercial purposes.
