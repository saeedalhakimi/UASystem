-- ErrorLog table for capturing stored procedure errors
CREATE TABLE dbo.ErrorLog (
    ErrorLogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    ErrorNumber INT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NOT NULL,
    ErrorProcedure NVARCHAR(128) NULL,
    ErrorLine INT NULL,
    ErrorSeverity INT NULL,
    ErrorState INT NULL,
    CorrelationId NVARCHAR(36) NULL, -- Stores GUID as string for application correlation
    ErrorTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(), -- Use DATETIME2 and UTC for consistency
    AdditionalInfo NVARCHAR(MAX) NULL, -- Optional details (e.g., parameter values)
	RowGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
);

-- Index for querying by ErrorTimestamp
CREATE NONCLUSTERED INDEX IX_ErrorLog_ErrorTimestamp ON dbo.ErrorLog(ErrorTimestamp);

-- Index for querying by CorrelationId
CREATE NONCLUSTERED INDEX IX_ErrorLog_CorrelationId ON dbo.ErrorLog(CorrelationId);

CREATE NONCLUSTERED INDEX IX_ErrorLog_ErrorNumber ON dbo.ErrorLog(ErrorNumber);
CREATE NONCLUSTERED INDEX IX_ErrorLog_ErrorProcedure ON dbo.ErrorLog(ErrorProcedure);

GO



--Create a maintenance job to purge old ErrorLog records (e.g., older than 90 days) to manage table growth
CREATE PROCEDURE dbo.PurgeErrorLog
    @DaysOld INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.ErrorLog
    WHERE ErrorTimestamp < DATEADD(DAY, -@DaysOld, GETUTCDATE());
END;
GO


