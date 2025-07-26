CREATE TABLE [dbo].[User] (
    UserId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    PersonId UNIQUEIDENTIFIER NOT NULL,
    UserName NVARCHAR(50) NOT NULL,
    NormalizedUserName AS UPPER(UserName) PERSISTED NOT NULL, -- Computed column for uppercase UserName
    Password NVARCHAR(256) NOT NULL, -- For hashed passwords (e.g., bcrypt, Argon2)
    Salt NVARCHAR(256) NOT NULL, -- Increased for future-proofing
    IsActive BIT NOT NULL DEFAULT 0,
    LastLoginAt DATETIME NULL, -- Track last successful login
    FailedLoginAttempts INT NOT NULL DEFAULT 0, -- Track failed login attempts
    IsLocked BIT NOT NULL DEFAULT 0, -- Account lockout status
    LockedAt DATETIME NULL, -- When account was locked
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedAt DATETIME NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy UNIQUEIDENTIFIER NULL,
    DeletedAt DATETIME NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_User PRIMARY KEY NONCLUSTERED (UserId),
    CONSTRAINT FK_User_Person FOREIGN KEY (PersonId) REFERENCES Person(PersonId),
    CONSTRAINT CHK_User_UserName CHECK (UserName <> '' AND UserName NOT LIKE '%[^a-zA-Z0-9_-]%'), -- Allow letters, numbers, underscores, hyphens
    CONSTRAINT CHK_User_NormalizedUserName CHECK (NormalizedUserName <> ''),
    CONSTRAINT CHK_User_Password CHECK (Password <> ''),
    CONSTRAINT CHK_User_Salt CHECK (Salt <> ''),
    CONSTRAINT CHK_User_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0),
    CONSTRAINT UQ_User_UserName UNIQUE (UserName),
    CONSTRAINT UQ_User_NormalizedUserName UNIQUE (NormalizedUserName),
    INDEX IX_User_IsDeleted (IsDeleted),
    INDEX IX_User_NormalizedUserName (NormalizedUserName), -- For username lookups
    INDEX IX_User_CreatedBy (CreatedBy) WHERE CreatedBy IS NOT NULL,
    INDEX IX_User_DeletedBy (DeletedBy) WHERE DeletedBy IS NOT NULL,
    INDEX IX_User_IsActive_IsLocked (IsActive, IsLocked) WHERE IsActive = 1 AND IsLocked = 0 -- For active, unlocked users
);
GO

CREATE OR ALTER TRIGGER [dbo].[TRG_User_UpdateIsActive]
ON [dbo].[User]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE(IsDeleted)
    BEGIN
        DECLARE @UpdatedBy UNIQUEIDENTIFIER = NEWID(); -- Simulate a user ID for updates
        DECLARE @UpdatedAt DATETIME = GETDATE();

        -- Update IsActive based on IsDeleted
        UPDATE u
        SET 
            u.IsActive = CASE WHEN i.IsDeleted = 1 THEN 0 ELSE 1 END,
            u.UpdatedAt = @UpdatedAt,
            u.UpdatedBy = @UpdatedBy
        FROM [dbo].[User] u
        INNER JOIN inserted i ON u.UserId = i.UserId
        INNER JOIN deleted d ON u.UserId = d.UserId
        WHERE i.IsDeleted <> d.IsDeleted; -- Only update if IsDeleted changed

        -- Log the IsActive update action
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
            'IsActive updated based on IsDeleted change',
            'TRG_User_UpdateIsActive',
            0,
            0,
            0,
            NEWID(),
            GETDATE(),
            (SELECT 
                i.UserId AS UserId,
                i.IsDeleted AS NewIsDeleted,
                i.IsActive AS NewIsActive,
                d.IsDeleted AS OldIsDeleted,
                d.IsActive AS OldIsActive,
                @UpdatedBy AS UpdatedBy,
                @UpdatedAt AS UpdatedAt
             FROM inserted i
             INNER JOIN deleted d ON i.UserId = d.UserId
             WHERE i.IsDeleted <> d.IsDeleted
             FOR JSON PATH) AS AdditionalInfo
        FROM inserted i
        INNER JOIN deleted d ON i.UserId = d.UserId
        WHERE i.IsDeleted <> d.IsDeleted;
    END;
END;
GO

