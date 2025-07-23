-- AuditLog table for capturing changes across all tables
CREATE TABLE dbo.AuditLog (
    AuditLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    TableName NVARCHAR(128) NOT NULL, -- Name of the table (e.g., Country, Users)
    Operation NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    PrimaryKeyValue NVARCHAR(128) NOT NULL, -- Primary key of the affected row (e.g., CountryID)
    OldValues NVARCHAR(MAX) NULL, -- JSON or text of old values (NULL for INSERT)
    NewValues NVARCHAR(MAX) NULL, -- JSON or text of new values (NULL for DELETE)
    ChangedBy NVARCHAR(100) NULL, -- User or system identifier (e.g., username, app ID)
    CorrelationId NVARCHAR(36) NULL, -- GUID for tracing (matches ErrorLog)
    ChangeTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(), -- UTC timestamp of change
    CONSTRAINT CHK_AuditLog_Operation CHECK (Operation IN ('INSERT', 'UPDATE', 'DELETE')),
    CONSTRAINT CHK_AuditLog_TableName CHECK (TableName <> '')
);

-- Indexes for common audit queries
CREATE NONCLUSTERED INDEX IX_AuditLog_TableName ON dbo.AuditLog(TableName);
CREATE NONCLUSTERED INDEX IX_AuditLog_ChangeTimestamp ON dbo.AuditLog(ChangeTimestamp);
CREATE NONCLUSTERED INDEX IX_AuditLog_CorrelationId ON dbo.AuditLog(CorrelationId);
CREATE NONCLUSTERED INDEX IX_AuditLog_Operation ON dbo.AuditLog(Operation);
GO

CREATE PROCEDURE dbo.PurgeAuditLog
    @DaysOld INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.AuditLog
    WHERE ChangeTimestamp < DATEADD(DAY, -@DaysOld, GETUTCDATE());
END;
GO