CREATE TABLE [dbo].[Role] (
    RoleId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    RoleName NVARCHAR(50) NOT NULL,
    NormalizedRoleName AS UPPER(RoleName) PERSISTED NOT NULL, -- Computed column for uppercase RoleName
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER NULL,
    UpdatedAt DATETIME NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedBy UNIQUEIDENTIFIER NULL,
    DeletedAt DATETIME NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_Role PRIMARY KEY NONCLUSTERED (RoleId),
    CONSTRAINT CHK_Role_RoleName CHECK (RoleName <> ''),
    CONSTRAINT CHK_Role_NormalizedRoleName CHECK (NormalizedRoleName <> ''),
    CONSTRAINT UQ_Role_RoleName UNIQUE (RoleName),
    CONSTRAINT UQ_Role_NormalizedRoleName UNIQUE (NormalizedRoleName),
    INDEX IX_Role_IsDeleted (IsDeleted),
    INDEX IX_Role_RoleName (RoleName),
    INDEX IX_Role_NormalizedRoleName (NormalizedRoleName),
    INDEX IX_Role_CreatedBy (CreatedBy) WHERE CreatedBy IS NOT NULL,
    INDEX IX_Role_DeletedBy (DeletedBy) WHERE DeletedBy IS NOT NULL
);
GO