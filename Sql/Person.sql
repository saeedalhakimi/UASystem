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


CREATE OR ALTER TRIGGER [dbo].[TRG_Person_InsteadOfDelete]
ON [dbo].[Person]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DeletedBy UNIQUEIDENTIFIER = NEWID(); -- Simulate a user ID for deletion
    DECLARE @DeletedAt DATETIME = GETDATE();

    -- Update Person records to soft delete
    UPDATE p
    SET 
        p.IsDeleted = 1,
        p.DeletedBy = @DeletedBy,
        p.DeletedAt = @DeletedAt,
        p.UpdatedAt = @DeletedAt,
        p.UpdatedBy = @DeletedBy
    FROM [dbo].[Person] p
    INNER JOIN deleted d ON p.PersonId = d.PersonId
    WHERE p.IsDeleted = 0;

    -- Log the soft delete action
    INSERT INTO [dbo].[ErrorLog] (
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
    SELECT 
        0, -- No error, just logging
        'Soft delete performed on Person',
        'TRG_Person_InsteadOfDelete',
        0,
        0,
        0,
        NEWID(),
        GETDATE(),
        (SELECT 
            d.PersonId AS PersonId,
            @DeletedBy AS DeletedBy,
            @DeletedAt AS DeletedAt
         FROM deleted d
         FOR JSON PATH) AS AdditionalInfo
    FROM deleted d;
END;
GO




----------------SPs
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
    @RowsAffected INT OUTPUT
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
        WHERE PersonId = @PersonId
            AND RowVersion = @RowVersion
            AND IsDeleted = 0;

        -- Set output parameter
        SET @RowsAffected = @@ROWCOUNT;

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
            'DELETE',
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
                CAST(p.RowVersion AS BINARY(8)) AS RowVersion
             FROM @OldValuesTable o
             INNER JOIN Person p ON p.PersonId = @PersonId
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


CREATE OR ALTER PROCEDURE SP_GetAllPersons
    @PageNumber INT,
    @PageSize INT,
    @SearchTerm NVARCHAR(100) = NULL,
    @SortBy NVARCHAR(50) = NULL,
    @SortDescending BIT = 0,
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
        IF @PageNumber < 1
            THROW 50010, 'PageNumber must be greater than 0.', 1;
        IF @PageSize < 1
            THROW 50010, 'PageSize must be greater than 0.', 1;
        IF @SortBy IS NOT NULL AND @SortBy != 'null' AND @SortBy NOT IN ('FirstName', 'LastName', 'CreatedAt', 'UpdatedAt')
            THROW 50011, 'SortBy must be one of: FirstName, LastName, CreatedAt, UpdatedAt.', 1;

        -- Build dynamic SQL for sorting
        DECLARE @SqlQuery NVARCHAR(MAX);
        SET @SqlQuery = N'
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
            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
                AND (@SearchTerm IS NULL OR @SearchTerm = ''null'' OR 
                     FirstName LIKE ''%'' + @SearchTerm + ''%'' OR 
                     LastName LIKE ''%'' + @SearchTerm + ''%'')
            ORDER BY ' + 
            CASE 
                WHEN @SortBy IS NULL OR @SortBy = 'null' THEN 'CreatedAt'
                ELSE @SortBy
            END + 
            CASE 
                WHEN @SortDescending = 1 THEN ' DESC'
                ELSE ' ASC'
            END + 
            N' OFFSET (@PageNumber - 1) * @PageSize ROWS
            FETCH NEXT @PageSize ROWS ONLY;';

        -- Execute the query
        EXEC sp_executesql @SqlQuery,
            N'@PageNumber INT, @PageSize INT, @SearchTerm NVARCHAR(100), @IncludeDeleted BIT',
            @PageNumber, @PageSize, @SearchTerm, @IncludeDeleted;

    END TRY
    BEGIN CATCH
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

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
                @PageNumber AS PageNumber,
                @PageSize AS PageSize,
                @SearchTerm AS SearchTerm,
                @SortBy AS SortBy,
                @SortDescending AS SortDescending,
                @IncludeDeleted AS IncludeDeleted,
                @CorrelationId AS CorrelationId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

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


CREATE OR ALTER PROCEDURE SP_GetPersonCount
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
        SELECT COUNT(*)
        FROM Person
        WHERE @IncludeDeleted = 1 OR IsDeleted = 0;
    END TRY
    BEGIN CATCH
        SELECT
            @ErrorNumber = ERROR_NUMBER(),
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorProcedure = ERROR_PROCEDURE(),
            @ErrorLine = ERROR_LINE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

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
                @IncludeDeleted AS IncludeDeleted,
                @CorrelationId AS CorrelationId
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        );

        THROW;
    END CATCH;
END;
GO





--------------insert dummy 

SET NOCOUNT ON;

-- Declare arrays for generating random data
DECLARE @Titles TABLE (Title NVARCHAR(10));
INSERT INTO @Titles (Title) VALUES ('Mr.'), ('Ms.'), ('Dr.'), ('Mrs.'), (NULL);

DECLARE @FirstNames TABLE (FirstName NVARCHAR(50));
INSERT INTO @FirstNames (FirstName) VALUES 
    ('John'), ('Jane'), ('Michael'), ('Emily'), ('David'), ('Sarah'), ('James'), ('Lisa'), ('Robert'), ('Anna'),
    ('William'), ('Emma'), ('Thomas'), ('Olivia'), ('Charles'), ('Sophia'), ('Joseph'), ('Ava'), ('Daniel'), ('Mia');

DECLARE @MiddleNames TABLE (MiddleName NVARCHAR(50));
INSERT INTO @MiddleNames (MiddleName) VALUES 
    ('Lee'), ('Marie'), ('Alan'), ('Rose'), ('Scott'), ('Lynn'), ('Paul'), ('Grace'), (NULL), (NULL);

DECLARE @LastNames TABLE (LastName NVARCHAR(50));
INSERT INTO @LastNames (LastName) VALUES 
    ('Smith'), ('Johnson'), ('Brown'), ('Taylor'), ('Wilson'), ('Davis'), ('Clark'), ('Harris'), ('Lewis'), ('Walker'),
    ('Hall'), ('Allen'), ('Young'), ('King'), ('Wright'), ('Scott'), ('Green'), ('Baker'), ('Adams'), ('Nelson');

DECLARE @Suffixes TABLE (Suffix NVARCHAR(10));
INSERT INTO @Suffixes (Suffix) VALUES ('Jr.'), ('Sr.'), ('III'), (NULL), (NULL);

-- Declare variables for loop and random data
DECLARE @Counter INT = 1;
DECLARE @MaxRecords INT = 200;
DECLARE @RandomFirstName NVARCHAR(50);
DECLARE @RandomMiddleName NVARCHAR(50);
DECLARE @RandomLastName NVARCHAR(50);
DECLARE @RandomTitle NVARCHAR(10);
DECLARE @RandomSuffix NVARCHAR(10);
DECLARE @RandomCreatedAt DATETIME;
DECLARE @RandomCreatedBy UNIQUEIDENTIFIER;
DECLARE @RandomUpdatedAt DATETIME;
DECLARE @RandomUpdatedBy UNIQUEIDENTIFIER;
DECLARE @RandomIsDeleted BIT;
DECLARE @RandomDeletedBy UNIQUEIDENTIFIER;
DECLARE @RandomDeletedAt DATETIME;

-- Begin transaction to ensure atomicity
BEGIN TRY
    BEGIN TRANSACTION;

    WHILE @Counter <= @MaxRecords
    BEGIN
        -- Generate random data
        SELECT TOP 1 @RandomFirstName = FirstName 
        FROM @FirstNames 
        ORDER BY NEWID();

        SELECT TOP 1 @RandomMiddleName = MiddleName 
        FROM @MiddleNames 
        ORDER BY NEWID();

        SELECT TOP 1 @RandomLastName = LastName 
        FROM @LastNames 
        ORDER BY NEWID();

        SELECT TOP 1 @RandomTitle = Title 
        FROM @Titles 
        ORDER BY NEWID();

        SELECT TOP 1 @RandomSuffix = Suffix 
        FROM @Suffixes 
        ORDER BY NEWID();

        -- Random CreatedAt between 2023-01-01 and current date
        SET @RandomCreatedAt = DATEADD(DAY, -ABS(CHECKSUM(NEWID()) % 912), GETDATE()); -- Up to ~2.5 years ago
        SET @RandomCreatedBy = CASE WHEN RAND() > 0.2 THEN NEWID() ELSE NULL END; -- 80% chance of having CreatedBy

        -- Random UpdatedAt (50% chance of being updated, after CreatedAt)
        SET @RandomUpdatedAt = CASE 
            WHEN RAND() > 0.5 THEN DATEADD(DAY, ABS(CHECKSUM(NEWID()) % DATEDIFF(DAY, @RandomCreatedAt, GETDATE())), @RandomCreatedAt)
            ELSE NULL 
        END;
        SET @RandomUpdatedBy = CASE WHEN @RandomUpdatedAt IS NOT NULL THEN NEWID() ELSE NULL END;

        -- Random IsDeleted (10% chance of being deleted)
        SET @RandomIsDeleted = CASE WHEN RAND() < 0.1 THEN 1 ELSE 0 END;
        SET @RandomDeletedBy = CASE WHEN @RandomIsDeleted = 1 THEN NEWID() ELSE NULL END;
        SET @RandomDeletedAt = CASE 
            WHEN @RandomIsDeleted = 1 THEN DATEADD(DAY, ABS(CHECKSUM(NEWID()) % DATEDIFF(DAY, @RandomCreatedAt, GETDATE())), @RandomCreatedAt)
            ELSE NULL 
        END;

        -- Insert record
        INSERT INTO Person (
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
            DeletedAt
        )
        VALUES (
            @RandomFirstName,
            @RandomMiddleName,
            @RandomLastName,
            @RandomTitle,
            @RandomSuffix,
            @RandomCreatedAt,
            @RandomCreatedBy,
            @RandomUpdatedAt,
            @RandomUpdatedBy,
            @RandomIsDeleted,
            @RandomDeletedBy,
            @RandomDeletedAt
        );

        SET @Counter = @Counter + 1;
    END;

    -- Commit transaction
    COMMIT TRANSACTION;
    PRINT 'Successfully inserted 200 dummy records into Person table.';
END TRY
BEGIN CATCH
    -- Rollback transaction on error
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    -- Log error
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Error occurred: ' + @ErrorMessage;
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO


select * from Person