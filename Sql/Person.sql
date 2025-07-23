CREATE TABLE Person (
    PersonId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    FirstName NVARCHAR(50) NOT NULL,
    MiddleName NVARCHAR(50) NULL,
    LastName NVARCHAR(50) NOT NULL,
    Title NVARCHAR(10) NULL, -- e.g., Mr., Ms., Dr.
    Suffix NVARCHAR(10) NULL, -- e.g., Jr., Sr.
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedAt DATETIME NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy UNIQUEIDENTIFIER NULL,
    DeletedAt DATETIME NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_Person PRIMARY KEY NONCLUSTERED (PersonId),
    CONSTRAINT CHK_Person_FirstName CHECK (FirstName <> ''),
    CONSTRAINT CHK_Person_LastName CHECK (LastName <> ''),
    INDEX IX_Person_IsDeleted (IsDeleted),
    INDEX IX_Person_FirstName_LastName (FirstName, LastName),
    INDEX IX_Person_CreatedBy (CreatedBy) WHERE CreatedBy IS NOT NULL,
    INDEX IX_Person_DeletedBy (DeletedBy) WHERE DeletedBy IS NOT NULL
);
GO

CREATE OR ALTER PROCEDURE SP_CreatePerson
    @PersonId UNIQUEIDENTIFIER,
    @FirstName NVARCHAR(50),
    @MiddleName NVARCHAR(50) = NULL,
    @LastName NVARCHAR(50),
    @Title NVARCHAR(10) = NULL,
    @Suffix NVARCHAR(10) = NULL,
    @CreatedBy UNIQUEIDENTIFIER = NULL,
    @CorrelationId NVARCHAR(36) = NULL,
    @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare table variable to capture OUTPUT clause for audit logging
    DECLARE @OutputTable TABLE (
        PersonId UNIQUEIDENTIFIER,
        RowVersion BINARY(8)
    );

    DECLARE @ErrorMessage NVARCHAR(MAX);
    DECLARE @ErrorNumber INT;
    DECLARE @ErrorLine INT;
    DECLARE @ErrorProcedure NVARCHAR(128);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @EffectiveCorrelationId NVARCHAR(36) = COALESCE(@CorrelationId, NEWID());

    BEGIN TRY
        -- Validate input parameters
        IF @PersonId IS NULL
            THROW 50005, 'PersonId cannot be NULL.', 1;
        IF LTRIM(RTRIM(@FirstName)) = ''
            THROW 50001, 'FirstName cannot be empty or whitespace.', 1;
        IF LTRIM(RTRIM(@LastName)) = ''
            THROW 50002, 'LastName cannot be empty or whitespace.', 1;

        -- Insert into Person table
        INSERT INTO Person (
            PersonId,
            FirstName,
            MiddleName,
            LastName,
            Title,
            Suffix,
            CreatedBy,
            CreatedAt,
            UpdatedAt,
            UpdatedBy,
            IsDeleted,
            DeletedBy,
            DeletedAt
        )
        OUTPUT inserted.PersonId, inserted.RowVersion
        INTO @OutputTable (PersonId, RowVersion)
        VALUES (
            @PersonId,
            @FirstName,
            @MiddleName,
            @LastName,
            @Title,
            @Suffix,
            @CreatedBy,
            GETDATE(),
            NULL,
            NULL,
            0,
            NULL,
            NULL
        );

        -- Set rows affected
        SET @RowsAffected = @@ROWCOUNT;

        -- Log to FinalAuditLog
        INSERT INTO FinalAuditLog (
            TableName,
            Operation,
            PrimaryKeyValue,
            NewValues,
            ChangedBy,
            CorrelationId,
            ChangeTimestamp
        )
        SELECT
            'Person',
            'INSERT',
            CAST(@PersonId AS NVARCHAR(128)),
            (SELECT
                @PersonId AS PersonId,
                @FirstName AS FirstName,
                @MiddleName AS MiddleName,
                @LastName AS LastName,
                @Title AS Title,
                @Suffix AS Suffix,
                GETDATE() AS CreatedAt,
                @CreatedBy AS CreatedBy,
                NULL AS UpdatedAt,
                NULL AS UpdatedBy,
                0 AS IsDeleted,
                NULL AS DeletedBy,
                NULL AS DeletedAt,
                CAST(o.RowVersion AS BINARY(8)) AS RowVersion
             FROM @OutputTable o
             WHERE o.PersonId = @PersonId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @CreatedBy,
            @EffectiveCorrelationId,
            GETDATE();

    END TRY
    BEGIN CATCH
        -- Capture error details
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Log error to ErrorLog
        INSERT INTO ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            ErrorTimestamp,
            AdditionalInfo
        )
        VALUES (
            @ErrorNumber,
            @ErrorMessage,
            @ErrorProcedure,
            @ErrorLine,
            @ErrorSeverity,
            @ErrorState,
            @EffectiveCorrelationId,
            GETUTCDATE(),
            (SELECT
                CAST(@PersonId AS NVARCHAR(128)) AS PersonId,
                @FirstName AS FirstName,
                @MiddleName AS MiddleName,
                @LastName AS LastName,
                @Title AS Title,
                @Suffix AS Suffix,
                CAST(@CreatedBy AS NVARCHAR(50)) AS CreatedBy,
                @CorrelationId AS CorrelationId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE SP_UpdatePerson
    @PersonId UNIQUEIDENTIFIER,
    @FirstName NVARCHAR(50),
    @MiddleName NVARCHAR(50) = NULL,
    @LastName NVARCHAR(50),
    @Title NVARCHAR(10) = NULL,
    @Suffix NVARCHAR(10) = NULL,
    @UpdatedBy UNIQUEIDENTIFIER = NULL,
    @CorrelationId NVARCHAR(36) = NULL,
    @RowVersion BINARY(8),
    @NewRowVersion BINARY(8) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare table variable to capture OUTPUT clause
    DECLARE @OutputTable TABLE (
        PersonId UNIQUEIDENTIFIER,
        RowVersion BINARY(8)
    );

    DECLARE @ErrorMessage NVARCHAR(MAX);
    DECLARE @ErrorNumber INT;
    DECLARE @ErrorLine INT;
    DECLARE @ErrorProcedure NVARCHAR(128);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @EffectiveCorrelationId NVARCHAR(36) = COALESCE(@CorrelationId, NEWID());

    -- Declare table to capture old values for audit
    DECLARE @OldValuesTable TABLE (
        FirstName NVARCHAR(50),
        MiddleName NVARCHAR(50),
        LastName NVARCHAR(50),
        Title NVARCHAR(10),
        Suffix NVARCHAR(10),
        CreatedAt DATETIME,
        CreatedBy UNIQUEIDENTIFIER,
        UpdatedAt DATETIME,
        UpdatedBy UNIQUEIDENTIFIER,
        IsDeleted BIT,
        DeletedBy UNIQUEIDENTIFIER,
        DeletedAt DATETIME,
        RowVersion BINARY(8)
    );

    BEGIN TRY
        -- Validate input parameters
        IF @PersonId IS NULL
            THROW 50005, 'PersonId cannot be NULL.', 1;
        IF @RowVersion IS NULL
            THROW 50006, 'RowVersion cannot be NULL.', 1;
        IF LTRIM(RTRIM(@FirstName)) = ''
            THROW 50001, 'FirstName cannot be empty or whitespace.', 1;
        IF LTRIM(RTRIM(@LastName)) = ''
            THROW 50002, 'LastName cannot be empty or whitespace.', 1;

        -- Capture old values for audit
        INSERT INTO @OldValuesTable
        SELECT
            FirstName,
            MiddleName,
            LastName,
            Title,
            Suffix,
            CreatedAt,
            CreatedBy,
            UpdatedAt,
            UpdatedBy,
            IsDeleted,
            DeletedBy,
            DeletedAt,
            RowVersion
        FROM Person
        WHERE PersonId = @PersonId AND RowVersion = @RowVersion;

        -- Check for concurrency conflict or non-existent record
        IF @@ROWCOUNT = 0
            THROW 50007, 'Concurrency conflict or record not found for the provided PersonId and RowVersion.', 1;

        -- Update Person table
        UPDATE Person
        SET
            FirstName = @FirstName,
            MiddleName = @MiddleName,
            LastName = @LastName,
            Title = @Title,
            Suffix = @Suffix,
            UpdatedAt = GETDATE(),
            UpdatedBy = @UpdatedBy
        OUTPUT inserted.PersonId, inserted.RowVersion
        INTO @OutputTable (PersonId, RowVersion)
        WHERE PersonId = @PersonId AND RowVersion = @RowVersion;

        -- Retrieve new RowVersion
        SELECT @NewRowVersion = RowVersion
        FROM @OutputTable
        WHERE PersonId = @PersonId;

        -- Log to FinalAuditLog
        INSERT INTO FinalAuditLog (
            TableName,
            Operation,
            PrimaryKeyValue,
            OldValues,
            NewValues,
            ChangedBy,
            CorrelationId,
            ChangeTimestamp
        )
        SELECT
            'Person',
            'UPDATE',
            CAST(@PersonId AS NVARCHAR(128)),
            (SELECT
                @PersonId AS PersonId,
                o.FirstName,
                o.MiddleName,
                o.LastName,
                o.Title,
                o.Suffix,
                o.CreatedAt,
                o.CreatedBy,
                o.UpdatedAt,
                o.UpdatedBy,
                o.IsDeleted,
                o.DeletedBy,
                o.DeletedAt,
                CAST(o.RowVersion AS BINARY(8)) AS RowVersion
             FROM @OldValuesTable o
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT
                @PersonId AS PersonId,
                @FirstName AS FirstName,
                @MiddleName AS MiddleName,
                @LastName AS LastName,
                @Title AS Title,
                @Suffix AS Suffix,
                o.CreatedAt,
                o.CreatedBy,
                GETDATE() AS UpdatedAt,
                @UpdatedBy AS UpdatedBy,
                o.IsDeleted,
                o.DeletedBy,
                o.DeletedAt,
                CAST(@NewRowVersion AS BINARY(8)) AS RowVersion
             FROM @OldValuesTable o
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @UpdatedBy,
            @EffectiveCorrelationId,
            GETDATE();

    END TRY
    BEGIN CATCH
        -- Capture error details
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Log error to ErrorLog
        INSERT INTO ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            ErrorTimestamp,
            AdditionalInfo
        )
        VALUES (
            @ErrorNumber,
            @ErrorMessage,
            @ErrorProcedure,
            @ErrorLine,
            @ErrorSeverity,
            @ErrorState,
            @EffectiveCorrelationId,
            GETUTCDATE(),
            (SELECT
                CAST(@PersonId AS NVARCHAR(128)) AS PersonId,
                @FirstName AS FirstName,
                @MiddleName AS MiddleName,
                @LastName AS LastName,
                @Title AS Title,
                @Suffix AS Suffix,
                CAST(@UpdatedBy AS NVARCHAR(50)) AS UpdatedBy,
                @CorrelationId AS CorrelationId,
                CAST(@RowVersion AS BINARY(8)) AS RowVersion
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE SP_DeletePerson
    @PersonId UNIQUEIDENTIFIER,
    @RowVersion BINARY(8),
    @DeletedBy UNIQUEIDENTIFIER,

    @CorrelationId NVARCHAR(36) = NULL,
    @NewRowVersion BINARY(8) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Declare table variable to capture OUTPUT clause
    DECLARE @OutputTable TABLE (
        PersonId UNIQUEIDENTIFIER,
        RowVersion BINARY(8)
    );

    DECLARE @ErrorMessage NVARCHAR(MAX);
    DECLARE @ErrorNumber INT;
    DECLARE @ErrorLine INT;
    DECLARE @ErrorProcedure NVARCHAR(128);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @EffectiveCorrelationId NVARCHAR(36) = COALESCE(@CorrelationId, NEWID());

    -- Declare table to capture old values for audit
    DECLARE @OldValuesTable TABLE (
        FirstName NVARCHAR(50),
        MiddleName NVARCHAR(50),
        LastName NVARCHAR(50),
        Title NVARCHAR(10),
        Suffix NVARCHAR(10),
        CreatedAt DATETIME,
        CreatedBy UNIQUEIDENTIFIER,
        UpdatedAt DATETIME,
        UpdatedBy UNIQUEIDENTIFIER,
        IsDeleted BIT,
        DeletedBy UNIQUEIDENTIFIER,
        DeletedAt DATETIME,
        RowVersion BINARY(8)
    );

    BEGIN TRY
        -- Validate input parameters
        IF @PersonId IS NULL
            THROW 50005, 'PersonId cannot be NULL.', 1;
        IF @RowVersion IS NULL
            THROW 50006, 'RowVersion cannot be NULL.', 1;
        IF @DeletedBy IS NULL
            THROW 50009, 'DeletedBy cannot be NULL.', 1;

        -- Capture old values for audit
        INSERT INTO @OldValuesTable
        SELECT
            FirstName,
            MiddleName,
            LastName,
            Title,
            Suffix,
            CreatedAt,
            CreatedBy,
            UpdatedAt,
            UpdatedBy,
            IsDeleted,
            DeletedBy,
            DeletedAt,
            RowVersion
        FROM Person
        WHERE PersonId = @PersonId
            AND RowVersion = @RowVersion
            AND IsDeleted = 0;

        -- Check for concurrency conflict, non-existent record, or already deleted
        IF @@ROWCOUNT = 0
            THROW 50007, 'Concurrency conflict, record not found, or person is already deleted.', 1;

        -- Update Person table for soft delete
        UPDATE Person
        SET
            IsDeleted = 1,
            DeletedBy = @DeletedBy,
            DeletedAt = GETDATE(),
            UpdatedAt = GETDATE(),
            UpdatedBy = @DeletedBy
        OUTPUT inserted.PersonId, inserted.RowVersion
        INTO @OutputTable (PersonId, RowVersion)
        WHERE PersonId = @PersonId
            AND RowVersion = @RowVersion
            AND IsDeleted = 0;

        -- Retrieve new RowVersion
        SELECT @NewRowVersion = RowVersion
        FROM @OutputTable
        WHERE PersonId = @PersonId;

        -- Log to FinalAuditLog
        INSERT INTO FinalAuditLog (
            TableName,
            Operation,
            PrimaryKeyValue,
            OldValues,
            NewValues,
            ChangedBy,
            CorrelationId,
            ChangeTimestamp
        )
        SELECT
            'Person',
            'UPDATE',
            CAST(@PersonId AS NVARCHAR(128)),
            (SELECT
                @PersonId AS PersonId,
                o.FirstName,
                o.MiddleName,
                o.LastName,
                o.Title,
                o.Suffix,
                o.CreatedAt,
                o.CreatedBy,
                o.UpdatedAt,
                o.UpdatedBy,
                o.IsDeleted,
                o.DeletedBy,
                o.DeletedAt,
                CAST(o.RowVersion AS BINARY(8)) AS RowVersion
             FROM @OldValuesTable o
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            (SELECT
                @PersonId AS PersonId,
                o.FirstName,
                o.MiddleName,
                o.LastName,
                o.Title,
                o.Suffix,
                o.CreatedAt,
                o.CreatedBy,
                GETDATE() AS UpdatedAt,
                @DeletedBy AS UpdatedBy,
                1 AS IsDeleted,
                @DeletedBy AS DeletedBy,
                GETDATE() AS DeletedAt,
                CAST(@NewRowVersion AS BINARY(8)) AS RowVersion
             FROM @OldValuesTable o
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
            @DeletedBy,
            @EffectiveCorrelationId,
            GETDATE();

    END TRY
    BEGIN CATCH
        -- Capture error details
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Log error to ErrorLog
        INSERT INTO ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            ErrorTimestamp,
            AdditionalInfo
        )
        VALUES (
            @ErrorNumber,
            @ErrorMessage,
            @ErrorProcedure,
            @ErrorLine,
            @ErrorSeverity,
            @ErrorState,
            @EffectiveCorrelationId,
            GETUTCDATE(),
            (SELECT
                CAST(@PersonId AS NVARCHAR(128)) AS PersonId,
                CAST(@DeletedBy AS NVARCHAR(50)) AS DeletedBy,
                CAST(@RowVersion AS BINARY(8)) AS RowVersion,
                @CorrelationId AS CorrelationId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO


CREATE OR ALTER PROCEDURE SP_GetPersonById
    @PersonId UNIQUEIDENTIFIER,
    @IncludeDeleted BIT = 0,
    @CorrelationId NVARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(MAX);
    DECLARE @ErrorNumber INT;
    DECLARE @ErrorLine INT;
    DECLARE @ErrorProcedure NVARCHAR(128);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;
    DECLARE @EffectiveCorrelationId NVARCHAR(36) = COALESCE(@CorrelationId, NEWID());

    BEGIN TRY
        -- Validate input parameters
        IF @PersonId IS NULL
            THROW 50005, 'PersonId cannot be NULL.', 1;

        -- Retrieve person
        IF EXISTS (
            SELECT 1
            FROM Person
            WHERE PersonId = @PersonId
                AND (@IncludeDeleted = 1 OR IsDeleted = 0)
        )
        BEGIN
            SELECT
                PersonId,
                FirstName,
                MiddleName,
                LastName,
                Title,
                Suffix,
                CreatedAt,
                CreatedBy,
                UpdatedAt,
                UpdatedBy,
                IsDeleted,
                DeletedBy,
                DeletedAt,
                RowVersion
            FROM Person
            WHERE PersonId = @PersonId
                AND (@IncludeDeleted = 1 OR IsDeleted = 0);
        END
    END TRY
    BEGIN CATCH
        -- Capture error details
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Log error to ErrorLog
        INSERT INTO ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            ErrorTimestamp,
            AdditionalInfo
        )
        VALUES (
            @ErrorNumber,
            @ErrorMessage,
            @ErrorProcedure,
            @ErrorLine,
            @ErrorSeverity,
            @ErrorState,
            @EffectiveCorrelationId,
            GETUTCDATE(),
            (SELECT
                CAST(@PersonId AS NVARCHAR(128)) AS PersonId,
                @IncludeDeleted AS IncludeDeleted,
                @CorrelationId AS CorrelationId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO

select * from Person