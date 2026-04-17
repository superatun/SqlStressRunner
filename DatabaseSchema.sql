-- SQL Server script for the normalized logging schema.
-- The app can create this automatically from the Connection tab using "Setup Database".

IF OBJECT_ID('dbo.vw_StressTestSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_StressTestSummary;
GO

IF OBJECT_ID('dbo.StressTestStoredProcedureLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StressTestStoredProcedureLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IterationId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        IterationNumber INT NOT NULL,
        StoredProcedureName NVARCHAR(200) NOT NULL,
        StoredProcedureOrder INT NOT NULL,
        Success BIT NOT NULL,
        ExecutionDurationMs BIGINT NOT NULL,
        Parameters NVARCHAR(MAX) NULL,
        ResponsePayload NVARCHAR(MAX) NULL,
        ResponseRowCount INT NULL,
        ResponseResultSetCount INT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ExecutedAt DATETIME2 NOT NULL
    );

    CREATE INDEX IX_StressTestStoredProcedureLog_IterationId ON dbo.StressTestStoredProcedureLog (IterationId);
    CREATE INDEX IX_StressTestStoredProcedureLog_RunId ON dbo.StressTestStoredProcedureLog (RunId);
    CREATE INDEX IX_StressTestStoredProcedureLog_StoredProcedureName ON dbo.StressTestStoredProcedureLog (StoredProcedureName);
    CREATE INDEX IX_StressTestStoredProcedureLog_ExecutedAt ON dbo.StressTestStoredProcedureLog (ExecutedAt);
END
GO

IF OBJECT_ID('dbo.StressTestIterationLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StressTestIterationLog
    (
        IterationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        IterationNumber INT NOT NULL,
        Success BIT NOT NULL,
        TotalDurationMs BIGINT NOT NULL,
        ExecutedAt DATETIME2 NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_StressTestIterationLog_RunId ON dbo.StressTestIterationLog (RunId);
    CREATE INDEX IX_StressTestIterationLog_IterationNumber ON dbo.StressTestIterationLog (IterationNumber);
    CREATE INDEX IX_StressTestIterationLog_ExecutedAt ON dbo.StressTestIterationLog (ExecutedAt);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_StressTestStoredProcedureLog_StressTestIterationLog'
)
BEGIN
    ALTER TABLE dbo.StressTestStoredProcedureLog
    ADD CONSTRAINT FK_StressTestStoredProcedureLog_StressTestIterationLog
        FOREIGN KEY (IterationId) REFERENCES dbo.StressTestIterationLog (IterationId);
END
GO

IF OBJECT_ID('dbo.StressTestLogSummaryLegacy', 'U') IS NOT NULL
    DROP TABLE dbo.StressTestLogSummaryLegacy;
GO

IF OBJECT_ID('dbo.StressTestLogSummary', 'U') IS NOT NULL
AND EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'StressTestLogSummary'
      AND COLUMN_NAME IN ('AverageSp1DurationMs', 'AverageSp2DurationMs')
)
BEGIN
    EXEC sp_rename 'dbo.StressTestLogSummary', 'StressTestLogSummaryLegacy';
END
GO

IF OBJECT_ID('dbo.StressTestLogSummary', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StressTestLogSummary
    (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        TotalIterations INT NOT NULL,
        SuccessfulIterations INT NOT NULL,
        FailedIterations INT NOT NULL,
        TotalDurationMs BIGINT NOT NULL,
        TpsPerIteration FLOAT NOT NULL,
        TpsPerStoredProcedure FLOAT NOT NULL,
        AverageLatencyMs FLOAT NOT NULL,
        MinLatencyMs BIGINT NOT NULL,
        MaxLatencyMs BIGINT NOT NULL,
        P95LatencyMs BIGINT NOT NULL,
        P99LatencyMs BIGINT NOT NULL,
        StartTime DATETIME2 NOT NULL,
        EndTime DATETIME2 NOT NULL
    );

    CREATE INDEX IX_StressTestLogSummary_StartTime ON dbo.StressTestLogSummary (StartTime);
END
GO

IF OBJECT_ID('dbo.StressTestLogSummaryLegacy', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.StressTestLogSummary
    (
        RunId, TotalIterations, SuccessfulIterations, FailedIterations, TotalDurationMs,
        TpsPerIteration, TpsPerStoredProcedure, AverageLatencyMs, MinLatencyMs, MaxLatencyMs,
        P95LatencyMs, P99LatencyMs, StartTime, EndTime
    )
    SELECT legacy.RunId, legacy.TotalIterations, legacy.SuccessfulIterations, legacy.FailedIterations, legacy.TotalDurationMs,
           legacy.TpsPerIteration, legacy.TpsPerStoredProcedure, legacy.AverageLatencyMs, legacy.MinLatencyMs, legacy.MaxLatencyMs,
           legacy.P95LatencyMs, legacy.P99LatencyMs, legacy.StartTime, legacy.EndTime
    FROM dbo.StressTestLogSummaryLegacy legacy
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.StressTestLogSummary currentRows
        WHERE currentRows.RunId = legacy.RunId
    );
END
GO

IF OBJECT_ID('dbo.StressTestLog', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.StressTestIterationLog
    (
        IterationId, RunId, IterationNumber, Success, TotalDurationMs, ExecutedAt, ErrorMessage
    )
    SELECT NEWID(), legacy.RunId, legacy.IterationNumber, legacy.Success, legacy.TotalDurationMs, legacy.ExecutedAt, legacy.ErrorMessage
    FROM dbo.StressTestLog legacy
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.StressTestIterationLog currentRows
        WHERE currentRows.RunId = legacy.RunId
          AND currentRows.IterationNumber = legacy.IterationNumber
    );
END
GO

IF OBJECT_ID('dbo.StressTestLog', 'U') IS NOT NULL
AND OBJECT_ID('dbo.StressTestLogLegacy', 'U') IS NULL
BEGIN
    EXEC sp_rename 'dbo.StressTestLog', 'StressTestLogLegacy';
END
GO

CREATE VIEW dbo.vw_StressTestSummary
AS
SELECT
    s.RunId,
    s.TotalIterations,
    s.SuccessfulIterations,
    s.FailedIterations,
    s.TotalDurationMs / 1000.0 AS TotalDurationSeconds,
    s.TpsPerIteration,
    s.TpsPerStoredProcedure,
    s.AverageLatencyMs,
    s.MinLatencyMs,
    s.MaxLatencyMs,
    s.P95LatencyMs,
    s.P99LatencyMs,
    s.StartTime,
    s.EndTime,
    COUNT(i.IterationId) AS LoggedIterations
FROM dbo.StressTestLogSummary s
LEFT JOIN dbo.StressTestIterationLog i ON s.RunId = i.RunId
GROUP BY
    s.RunId, s.TotalIterations, s.SuccessfulIterations, s.FailedIterations,
    s.TotalDurationMs, s.TpsPerIteration, s.TpsPerStoredProcedure,
    s.AverageLatencyMs, s.MinLatencyMs, s.MaxLatencyMs,
    s.P95LatencyMs, s.P99LatencyMs, s.StartTime, s.EndTime;
GO

-- Example queries
-- SELECT * FROM dbo.vw_StressTestSummary WHERE RunId = 'YOUR-GUID-HERE';
-- SELECT * FROM dbo.StressTestIterationLog WHERE RunId = 'YOUR-GUID-HERE' ORDER BY IterationNumber;
-- SELECT * FROM dbo.StressTestStoredProcedureLog WHERE IterationId = 'YOUR-ITERATION-ID' ORDER BY StoredProcedureOrder;
-- SELECT StoredProcedureName, AVG(ExecutionDurationMs) AS AvgDurationMs
-- FROM dbo.StressTestStoredProcedureLog
-- WHERE RunId = 'YOUR-GUID-HERE'
-- GROUP BY StoredProcedureName;
