-- Create generic AuditLog table to track changes across tables
CREATE TABLE FinalAuditLog (
    AuditLogId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TableName NVARCHAR(128) NOT NULL,
    Operation NVARCHAR(20) NOT NULL,
    PrimaryKeyValue NVARCHAR(128) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    ChangedBy UNIQUEIDENTIFIER NULL,
    CorrelationId NVARCHAR(36) NULL,
    ChangeTimestamp DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_FinalAuditLog PRIMARY KEY NONCLUSTERED (AuditLogId),
    CONSTRAINT CHK_FinalAuditLog_Operation CHECK (Operation IN ('INSERT', 'UPDATE', 'DELETE')),
    CONSTRAINT CHK_FinalAuditLog_TableName CHECK (TableName <> ''),
    CONSTRAINT CHK_FinalAuditLog_PrimaryKeyValue CHECK (PrimaryKeyValue <> ''),
    INDEX IX_FinalAuditLog_TableName_PrimaryKeyValue (TableName, PrimaryKeyValue),
    INDEX IX_FinalAuditLog_ChangeTimestamp (ChangeTimestamp)
);
GO
