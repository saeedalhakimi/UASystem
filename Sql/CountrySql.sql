-- Stores country information
CREATE TABLE Country (
    CountryID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CountryCode NVARCHAR(3) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Continent NVARCHAR(50) NULL,
    Capital NVARCHAR(100) NULL,
    CurrencyCode NVARCHAR(3) NULL, -- Reverted to NVARCHAR(3) for ISO 4217
    CountryDialNumber NVARCHAR(7) NULL, -- Increased to NVARCHAR(7) for flexibility
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    RowGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    CONSTRAINT CHK_Country_Code CHECK (CountryCode <> ''),
    CONSTRAINT CHK_Country_Code_Format CHECK (CountryCode LIKE '[A-Z][A-Z]' OR CountryCode LIKE '[A-Z][A-Z][A-Z]'),
    CONSTRAINT CHK_Country_Name CHECK (Name <> ''),
    CONSTRAINT UQ_Country_Name UNIQUE (Name),
    CONSTRAINT CHK_Country_CurrencyCode CHECK (CurrencyCode IS NULL OR CurrencyCode <> ''),
    CONSTRAINT CHK_Country_DialNumber CHECK (CountryDialNumber IS NULL OR CountryDialNumber LIKE '+[0-9]%')
);

-- Trigger for automatic UpdatedAt and IsDeleted/DeletedAt consistency
CREATE TRIGGER TRG_Country_UpdateTimestamp
ON Country
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Country
    SET UpdatedAt = GETUTCDATE(),
        DeletedAt = CASE 
            WHEN i.IsDeleted = 1 AND c.DeletedAt IS NULL THEN GETUTCDATE()
            WHEN i.IsDeleted = 0 THEN NULL
            ELSE c.DeletedAt 
        END
    FROM Country c
    INNER JOIN inserted i ON c.CountryID = i.CountryID;
END;
GO

-- Indexes
CREATE UNIQUE NONCLUSTERED INDEX IX_Country_CountryCode ON Country(CountryCode);
CREATE NONCLUSTERED INDEX IX_Country_Name ON Country(Name);
CREATE NONCLUSTERED INDEX IX_Country_IsDeleted ON Country(IsDeleted);
CREATE NONCLUSTERED INDEX IX_Country_RowGuid ON Country(RowGuid);
CREATE NONCLUSTERED INDEX IX_Country_Continent ON Country(Continent);
CREATE NONCLUSTERED INDEX IX_Country_Active ON Country(CountryID) WHERE IsDeleted = 0;
-- Optional: Add if CurrencyCode queries are frequent
CREATE NONCLUSTERED INDEX IX_Country_CurrencyCode ON Country(CurrencyCode);


---
-- Sample trigger for Country table to log changes to AuditLog
CREATE TRIGGER TRG_Country_Audit
ON Country
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @CorrelationId NVARCHAR(36) = NEWID();

        -- Handle INSERT
        IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
        BEGIN
            INSERT INTO dbo.AuditLog (
                TableName, Operation, PrimaryKeyValue, NewValues, ChangedBy, CorrelationId
            )
            SELECT
                'Country',
                'INSERT',
                CAST(i.CountryID AS NVARCHAR(128)),
                (SELECT 
                    CountryID, CountryCode, Name, Continent, Capital, CurrencyCode, 
                    CountryDialNumber, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, RowGuid
                 FROM inserted WHERE CountryID = i.CountryID FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                SYSTEM_USER, -- Replace with app-provided user if available
                @CorrelationId
            FROM inserted i;
        END

        -- Handle UPDATE
        IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        BEGIN
            INSERT INTO dbo.AuditLog (
                TableName, Operation, PrimaryKeyValue, OldValues, NewValues, ChangedBy, CorrelationId
            )
            SELECT
                'Country',
                'UPDATE',
                CAST(i.CountryID AS NVARCHAR(128)),
                (SELECT 
                    CountryID, CountryCode, Name, Continent, Capital, CurrencyCode, 
                    CountryDialNumber, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, RowGuid
                 FROM deleted WHERE CountryID = i.CountryID FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                (SELECT 
                    CountryID, CountryCode, Name, Continent, Capital, CurrencyCode, 
                    CountryDialNumber, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, RowGuid
                 FROM inserted WHERE CountryID = i.CountryID FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                SYSTEM_USER,
                @CorrelationId
            FROM inserted i
            INNER JOIN deleted d ON i.CountryID = d.CountryID;
        END

        -- Handle DELETE
        IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
        BEGIN
            INSERT INTO dbo.AuditLog (
                TableName, Operation, PrimaryKeyValue, OldValues, ChangedBy, CorrelationId
            )
            SELECT
                'Country',
                'DELETE',
                CAST(d.CountryID AS NVARCHAR(128)),
                (SELECT 
                    CountryID, CountryCode, Name, Continent, Capital, CurrencyCode, 
                    CountryDialNumber, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, RowGuid
                 FROM deleted WHERE CountryID = d.CountryID FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                SYSTEM_USER,
                @CorrelationId
            FROM deleted d;
        END
    END TRY
    BEGIN CATCH
        -- Log trigger errors to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber, ErrorMessage, ErrorProcedure, ErrorLine, 
            ErrorSeverity, ErrorState, CorrelationId
        )
        VALUES (
            ERROR_NUMBER(), ERROR_MESSAGE(), ERROR_PROCEDURE(), ERROR_LINE(),
            ERROR_SEVERITY(), ERROR_STATE(), @CorrelationId
        );
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_CreateCountry
    @CountryCode NVARCHAR(3),
    @Name NVARCHAR(100),
    @Continent NVARCHAR(50) = NULL,
    @Capital NVARCHAR(100) = NULL,
    @CurrencyCode NVARCHAR(3) = NULL,
    @CountryDialNumber NVARCHAR(7) = NULL,
    @CorrelationId NVARCHAR(36) = NULL,
    @CountryId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Validate inputs
        IF @Name IS NULL OR @Name = ''
        BEGIN
            THROW 50001, 'Country name cannot be empty', 1;
            RETURN;
        END

        IF @CountryCode IS NULL OR @CountryCode = ''
        BEGIN
            THROW 50002, 'Country code cannot be empty', 1;
            RETURN;
        END

        IF NOT (@CountryCode LIKE '[A-Z][A-Z]' OR @CountryCode LIKE '[A-Z][A-Z][A-Z]')
        BEGIN
            THROW 50003, 'Country code must be 2 or 3 uppercase letters', 1;
            RETURN;
        END

        IF @CurrencyCode IS NOT NULL AND @CurrencyCode = ''
        BEGIN
            THROW 50004, 'Currency code cannot be empty if provided', 1;
            RETURN;
        END

        IF @CountryDialNumber IS NOT NULL AND NOT (@CountryDialNumber LIKE '+[0-9]%')
        BEGIN
            THROW 50005, 'Country dial number must start with + followed by digits', 1;
            RETURN;
        END

        -- Check for duplicate name or code (only active records)
        IF EXISTS (SELECT 1 FROM dbo.Country WHERE Name = @Name AND IsDeleted = 0)
        BEGIN
            THROW 50006, 'Country name already exists', 1;
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM dbo.Country WHERE CountryCode = @CountryCode AND IsDeleted = 0)
        BEGIN
            THROW 50007, 'Country code already exists', 1;
            RETURN;
        END

        -- Insert new country
        INSERT INTO dbo.Country (
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted,
            DeletedAt,
            RowGuid
        )
        VALUES (
            @CountryCode,
            @Name,
            @Continent,
            @Capital,
            @CurrencyCode,
            @CountryDialNumber,
            GETUTCDATE(),
            GETUTCDATE(),
            0,
            NULL,
            NEWID()
        );

        -- Return the new CountryId
        SET @CountryId = SCOPE_IDENTITY();
    END TRY
    BEGIN CATCH
        -- Log error to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            AdditionalInfo
        )
        VALUES (
            ERROR_NUMBER(),
            ERROR_MESSAGE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @CorrelationId,
            'Failed to create country'
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE dbo.SP_UpdateCountry
    @CountryId INT,
    @CountryCode VARCHAR(3),
    @Name VARCHAR(100),
    @Continent VARCHAR(50) = NULL,
    @Capital VARCHAR(100) = NULL,
    @CurrencyCode VARCHAR(3) = NULL,
    @CountryDialNumber VARCHAR(20) = NULL,
    @CorrelationId VARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorNumber INT = 50001;

    BEGIN TRY
        -- Validate inputs
        IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = ''
        BEGIN
            SET @ErrorMessage = 'Country name cannot be empty';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF LEN(@Name) > 100
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 1;
            SET @ErrorMessage = 'Country name cannot exceed 100 characters';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF @CountryCode IS NULL OR LTRIM(RTRIM(@CountryCode)) = ''
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 2;
            SET @ErrorMessage = 'Country code cannot be empty';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF NOT (@CountryCode LIKE '[A-Z][A-Z]' OR @CountryCode LIKE '[A-Z][A-Z][A-Z]')
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 3;
            SET @ErrorMessage = 'Country code must be 2 or 3 uppercase letters';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF @CurrencyCode IS NOT NULL AND NOT @CurrencyCode LIKE '[A-Z][A-Z][A-Z]'
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 4;
            SET @ErrorMessage = 'Currency code must be 3 uppercase letters';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF @CountryDialNumber IS NOT NULL AND NOT @CountryDialNumber LIKE '+[0-9]%'
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 5;
            SET @ErrorMessage = 'Country dial number must start with ''+'' followed by digits';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Check for duplicate country name (excluding current CountryId)
        IF EXISTS (SELECT 1 FROM Country WHERE Name = @Name AND CountryId != @CountryId AND IsDeleted = 0)
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 6;
            SET @ErrorMessage = 'Country name already exists';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Check for duplicate country code (excluding current CountryId)
        IF EXISTS (SELECT 1 FROM Country WHERE CountryCode = @CountryCode AND CountryId != @CountryId AND IsDeleted = 0)
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 7;
            SET @ErrorMessage = 'Country code already exists';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Check if CountryId exists and is not deleted
        IF NOT EXISTS (SELECT 1 FROM Country WHERE CountryId = @CountryId AND IsDeleted = 0)
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 8;
            SET @ErrorMessage = 'Country not found or has been deleted';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Update the country
        UPDATE Country
        SET
            CountryCode = @CountryCode,
            Name = @Name,
            Continent = @Continent,
            Capital = @Capital,
            CurrencyCode = @CurrencyCode,
            CountryDialNumber = @CountryDialNumber,
            UpdatedAt = GETUTCDATE()
        WHERE CountryId = @CountryId AND IsDeleted = 0;

        -- Return the updated CountryId
        SELECT @CountryId AS CountryId;

    END TRY
    BEGIN CATCH
        -- Log error to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber, ErrorMessage, ErrorProcedure, ErrorLine, 
            ErrorSeverity, ErrorState, CorrelationId
        )
        VALUES (
            ERROR_NUMBER(), ERROR_MESSAGE(), ERROR_PROCEDURE(), ERROR_LINE(),
            ERROR_SEVERITY(), ERROR_STATE(), @CorrelationId
        );

        THROW;
    END CATCH
END
GO

-- Stored procedure to retrieve all active countries
CREATE OR ALTER PROCEDURE dbo.SP_GetAllCountries
    @CorrelationId NVARCHAR(36) = NULL -- Optional GUID for tracing
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Select all active countries (IsDeleted = 0)
        SELECT 
            CountryID,
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted
        FROM dbo.Country
        WHERE IsDeleted = 0
        ORDER BY Name; -- Order by Name for consistent results
    END TRY
    BEGIN CATCH
        -- Log error to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            AdditionalInfo
        )
        VALUES (
            ERROR_NUMBER(),
            ERROR_MESSAGE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @CorrelationId,
            'Failed to retrieve all countries'
        );

        -- Re-throw the error to the caller
        THROW;
    END CATCH;
END;
GO


CREATE OR ALTER PROCEDURE dbo.SP_GetCountriesByCurrencyCode
    @CurrencyCode VARCHAR(3),
    @CorrelationId VARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorNumber INT = 50001;

    BEGIN TRY
        -- Validate inputs
        IF @CurrencyCode IS NULL OR LTRIM(RTRIM(@CurrencyCode)) = ''
        BEGIN
            SET @ErrorMessage = 'Currency code cannot be empty';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        IF NOT @CurrencyCode LIKE '[A-Z][A-Z][A-Z]'
        BEGIN
            SET @ErrorNumber = @ErrorNumber + 1;
            SET @ErrorMessage = 'Currency code must be 3 uppercase letters';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Select countries by currency code
        SELECT
            CountryId,
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted
        FROM Country
        WHERE CurrencyCode = @CurrencyCode AND IsDeleted = 0;

    END TRY
    BEGIN CATCH
        -- Log error to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber, ErrorMessage, ErrorProcedure, ErrorLine, 
            ErrorSeverity, ErrorState, CorrelationId
        )
        VALUES (
            ERROR_NUMBER(), ERROR_MESSAGE(), ERROR_PROCEDURE(), ERROR_LINE(),
            ERROR_SEVERITY(), ERROR_STATE(), @CorrelationId
        );

        THROW;
    END CATCH
END
GO




USE [UASystemDb]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetCountryById]
    @CountryID INT,
    @CorrelationId NVARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            CountryID,
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted
        FROM dbo.Country
        WHERE CountryID = @CountryID AND IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            AdditionalInfo
        )
        VALUES (
            ERROR_NUMBER(),
            ERROR_MESSAGE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @CorrelationId,
            'Failed to retrieve country by ID'
        );
        THROW;
    END CATCH;
END;
GO

USE [UASystemDb]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER PROCEDURE [dbo].[SP_GetCountryByCode]
    @CountryCode NVARCHAR(10),
    @CorrelationId NVARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            CountryID,
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted
        FROM dbo.Country
        WHERE CountryCode = @CountryCode AND IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            AdditionalInfo
        )
        VALUES (
            ERROR_NUMBER(),
            ERROR_MESSAGE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @CorrelationId,
            'Failed to retrieve country by code'
        );
        THROW;
    END CATCH;
END;
GO

USE [UASystemDb]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SP_GetCountryByName]
    @Name NVARCHAR(100),
    @CorrelationId NVARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            CountryID,
            CountryCode,
            Name,
            Continent,
            Capital,
            CurrencyCode,
            CountryDialNumber,
            CreatedAt,
            UpdatedAt,
            IsDeleted
        FROM dbo.Country
        WHERE Name = @Name AND IsDeleted = 0;
    END TRY
    BEGIN CATCH
        INSERT INTO dbo.ErrorLog (
            ErrorNumber,
            ErrorMessage,
            ErrorProcedure,
            ErrorLine,
            ErrorSeverity,
            ErrorState,
            CorrelationId,
            AdditionalInfo
        )
        VALUES (
            ERROR_NUMBER(),
            ERROR_MESSAGE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            @CorrelationId,
            'Failed to retrieve country by name'
        );
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.SP_DeleteCountry
    @CountryId INT,
    @CorrelationId VARCHAR(36) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorNumber INT = 50001;

    BEGIN TRY
        -- Check if CountryId exists and is not deleted
        IF NOT EXISTS (SELECT 1 FROM Country WHERE CountryId = @CountryId AND IsDeleted = 0)
        BEGIN
            SET @ErrorMessage = 'Country not found or has been deleted';
            THROW @ErrorNumber, @ErrorMessage, 1;
        END

        -- Perform soft delete
        UPDATE Country
        SET
            IsDeleted = 1,
            DeletedAt = GETUTCDATE(),
            UpdatedAt = GETUTCDATE()
        WHERE CountryId = @CountryId AND IsDeleted = 0;

        -- Return the deleted CountryId
        SELECT @CountryId AS CountryId;

    END TRY
    BEGIN CATCH
        -- Log error to ErrorLog table
        INSERT INTO dbo.ErrorLog (
            ErrorNumber, ErrorMessage, ErrorProcedure, ErrorLine, 
            ErrorSeverity, ErrorState, CorrelationId
        )
        VALUES (
            ERROR_NUMBER(), ERROR_MESSAGE(), ERROR_PROCEDURE(), ERROR_LINE(),
            ERROR_SEVERITY(), ERROR_STATE(), @CorrelationId
        );

        THROW;
    END CATCH
END
GO


















-- Populate Country table with all 250 countries and territories (ISO 3166-1, 2025 data)
INSERT INTO Country (CountryCode, Name, Continent, Capital, CurrencyCode, CountryDialNumber)
VALUES
    ('AFG', 'Afghanistan', 'Asia', 'Kabul', 'AFN', '+93'),
    ('ALA', 'Aland Islands', 'Europe', 'Mariehamn', 'EUR', '+358'),
    ('ALB', 'Albania', 'Europe', 'Tirana', 'ALL', '+355'),
    ('DZA', 'Algeria', 'Africa', 'Algiers', 'DZD', '+213'),
    ('ASM', 'American Samoa', 'Oceania', 'Pago Pago', 'USD', '+1684'),
    ('AND', 'Andorra', 'Europe', 'Andorra la Vella', 'EUR', '+376'),
    ('AGO', 'Angola', 'Africa', 'Luanda', 'AOA', '+244'),
    ('AIA', 'Anguilla', 'North America', 'The Valley', 'XCD', '+1264'),
    ('ATA', 'Antarctica', 'Antarctica', NULL, NULL, NULL),
    ('ATG', 'Antigua and Barbuda', 'North America', 'Saint John''s', 'XCD', '+1268'),
    ('ARG', 'Argentina', 'South America', 'Buenos Aires', 'ARS', '+54'),
    ('ARM', 'Armenia', 'Asia', 'Yerevan', 'AMD', '+374'),
    ('ABW', 'Aruba', 'North America', 'Oranjestad', 'AWG', '+297'),
    ('AUS', 'Australia', 'Oceania', 'Canberra', 'AUD', '+61'),
    ('AUT', 'Austria', 'Europe', 'Vienna', 'EUR', '+43'),
    ('AZE', 'Azerbaijan', 'Asia', 'Baku', 'AZN', '+994'),
    ('BHS', 'Bahamas', 'North America', 'Nassau', 'BSD', '+1242'),
    ('BHR', 'Bahrain', 'Asia', 'Manama', 'BHD', '+973'),
    ('BGD', 'Bangladesh', 'Asia', 'Dhaka', 'BDT', '+880'),
    ('BRB', 'Barbados', 'North America', 'Bridgetown', 'BBD', '+1246'),
    ('BLR', 'Belarus', 'Europe', 'Minsk', 'BYN', '+375'),
    ('BEL', 'Belgium', 'Europe', 'Brussels', 'EUR', '+32'),
    ('BLZ', 'Belize', 'North America', 'Belmopan', 'BZD', '+501'),
    ('BEN', 'Benin', 'Africa', 'Porto-Novo', 'XOF', '+229'),
    ('BMU', 'Bermuda', 'North America', 'Hamilton', 'BMD', '+1441'),
    ('BTN', 'Bhutan', 'Asia', 'Thimphu', 'BTN', '+975'),
    ('BOL', 'Bolivia', 'South America', 'Sucre', 'BOB', '+591'),
    ('BES', 'Bonaire, Sint Eustatius and Saba', 'North America', 'Kralendijk', 'USD', '+599'),
    ('BIH', 'Bosnia and Herzegovina', 'Europe', 'Sarajevo', 'BAM', '+387'),
    ('BWA', 'Botswana', 'Africa', 'Gaborone', 'BWP', '+267'),
    ('BVT', 'Bouvet Island', 'Antarctica', NULL, 'NOK', NULL),
    ('BRA', 'Brazil', 'South America', 'Brasilia', 'BRL', '+55'),
    ('IOT', 'British Indian Ocean Territory', 'Asia', 'Diego Garcia', 'USD', '+246'),
    ('BRN', 'Brunei Darussalam', 'Asia', 'Bandar Seri Begawan', 'BND', '+673'),
    ('BGR', 'Bulgaria', 'Europe', 'Sofia', 'BGN', '+359'),
    ('BFA', 'Burkina Faso', 'Africa', 'Ouagadougou', 'XOF', '+226'),
    ('BDI', 'Burundi', 'Africa', 'Gitega', 'BIF', '+257'),
    ('CPV', 'Cabo Verde', 'Africa', 'Praia', 'CVE', '+238'),
    ('KHM', 'Cambodia', 'Asia', 'Phnom Penh', 'KHR', '+855'),
    ('CMR', 'Cameroon', 'Africa', 'Yaounde', 'XAF', '+237'),
    ('CAN', 'Canada', 'North America', 'Ottawa', 'CAD', '+1'),
    ('CYM', 'Cayman Islands', 'North America', 'George Town', 'KYD', '+1345'),
    ('CAF', 'Central African Republic', 'Africa', 'Bangui', 'XAF', '+236'),
    ('TCD', 'Chad', 'Africa', 'N''Djamena', 'XAF', '+235'),
    ('CHL', 'Chile', 'South America', 'Santiago', 'CLP', '+56'),
    ('CHN', 'China', 'Asia', 'Beijing', 'CNY', '+86'),
    ('CXR', 'Christmas Island', 'Oceania', 'Flying Fish Cove', 'AUD', '+61'),
    ('CCK', 'Cocos (Keeling) Islands', 'Oceania', 'West Island', 'AUD', '+61'),
    ('COL', 'Colombia', 'South America', 'Bogota', 'COP', '+57'),
    ('COM', 'Comoros', 'Africa', 'Moroni', 'KMF', '+269'),
    ('COD', 'Congo, Democratic Republic of the', 'Africa', 'Kinshasa', 'CDF', '+243'),
    ('COG', 'Congo', 'Africa', 'Brazzaville', 'XAF', '+242'),
    ('COK', 'Cook Islands', 'Oceania', 'Avarua', 'NZD', '+682'),
    ('CRI', 'Costa Rica', 'North America', 'San Jose', 'CRC', '+506'),
    ('HRV', 'Croatia', 'Europe', 'Zagreb', 'EUR', '+385'),
    ('CUB', 'Cuba', 'North America', 'Havana', 'CUP', '+53'),
    ('CUW', 'Curacao', 'North America', 'Willemstad', 'ANG', '+599'),
    ('CYP', 'Cyprus', 'Asia', 'Nicosia', 'EUR', '+357'),
    ('CZE', 'Czechia', 'Europe', 'Prague', 'CZK', '+420'),
    ('DNK', 'Denmark', 'Europe', 'Copenhagen', 'DKK', '+45'),
    ('DJI', 'Djibouti', 'Africa', 'Djibouti', 'DJF', '+253'),
    ('DMA', 'Dominica', 'North America', 'Roseau', 'XCD', '+1767'),
    ('DOM', 'Dominican Republic', 'North America', 'Santo Domingo', 'DOP', '+1809'),
    ('ECU', 'Ecuador', 'South America', 'Quito', 'USD', '+593'),
    ('EGY', 'Egypt', 'Africa', 'Cairo', 'EGP', '+20'),
    ('SLV', 'El Salvador', 'North America', 'San Salvador', 'USD', '+503'),
    ('GNQ', 'Equatorial Guinea', 'Africa', 'Malabo', 'XAF', '+240'),
    ('ERI', 'Eritrea', 'Africa', 'Asmara', 'ERN', '+291'),
    ('EST', 'Estonia', 'Europe', 'Tallinn', 'EUR', '+372'),
    ('SWZ', 'Eswatini', 'Africa', 'Mbabane', 'SZL', '+268'),
    ('ETH', 'Ethiopia', 'Africa', 'Addis Ababa', 'ETB', '+251'),
    ('FLK', 'Falkland Islands (Malvinas)', 'South America', 'Stanley', 'FKP', '+500'),
    ('FRO', 'Faroe Islands', 'Europe', 'Torshavn', 'DKK', '+298'),
    ('FJI', 'Fiji', 'Oceania', 'Suva', 'FJD', '+679'),
    ('FIN', 'Finland', 'Europe', 'Helsinki', 'EUR', '+358'),
    ('FRA', 'France', 'Europe', 'Paris', 'EUR', '+33'),
    ('GUF', 'French Guiana', 'South America', 'Cayenne', 'EUR', '+594'),
    ('PYF', 'French Polynesia', 'Oceania', 'Papeete', 'XPF', '+689'),
    ('ATF', 'French Southern Territories', 'Antarctica', 'Port-aux-Francais', 'EUR', NULL),
    ('GAB', 'Gabon', 'Africa', 'Libreville', 'XAF', '+241'),
    ('GMB', 'Gambia', 'Africa', 'Banjul', 'GMD', '+220'),
    ('GEO', 'Georgia', 'Asia', 'Tbilisi', 'GEL', '+995'),
    ('DEU', 'Germany', 'Europe', 'Berlin', 'EUR', '+49'),
    ('GHA', 'Ghana', 'Africa', 'Accra', 'GHS', '+233'),
    ('GIB', 'Gibraltar', 'Europe', 'Gibraltar', 'GIP', '+350'),
    ('GRC', 'Greece', 'Europe', 'Athens', 'EUR', '+30'),
    ('GRL', 'Greenland', 'North America', 'Nuuk', 'DKK', '+299'),
    ('GRD', 'Grenada', 'North America', 'Saint George''s', 'XCD', '+1473'),
    ('GLP', 'Guadeloupe', 'North America', 'Basse-Terre', 'EUR', '+590'),
    ('GUM', 'Guam', 'Oceania', 'Hagatna', 'USD', '+1671'),
    ('GTM', 'Guatemala', 'North America', 'Guatemala City', 'GTQ', '+502'),
    ('GGY', 'Guernsey', 'Europe', 'Saint Peter Port', 'GBP', '+44'),
    ('GIN', 'Guinea', 'Africa', 'Conakry', 'GNF', '+224'),
    ('GNB', 'Guinea-Bissau', 'Africa', 'Bissau', 'XOF', '+245'),
    ('GUY', 'Guyana', 'South America', 'Georgetown', 'GYD', '+592'),
    ('HTI', 'Haiti', 'North America', 'Port-au-Prince', 'HTG', '+509'),
    ('HMD', 'Heard Island and McDonald Islands', 'Antarctica', NULL, 'AUD', NULL),
    ('VAT', 'Holy See', 'Europe', 'Vatican City', 'EUR', '+39'),
    ('HND', 'Honduras', 'North America', 'Tegucigalpa', 'HNL', '+504'),
    ('HKG', 'Hong Kong', 'Asia', 'Hong Kong', 'HKD', '+852'),
    ('HUN', 'Hungary', 'Europe', 'Budapest', 'HUF', '+36'),
    ('ISL', 'Iceland', 'Europe', 'Reykjavik', 'ISK', '+354'),
    ('IND', 'India', 'Asia', 'New Delhi', 'INR', '+91'),
    ('IDN', 'Indonesia', 'Asia', 'Jakarta', 'IDR', '+62'),
    ('IRN', 'Iran', 'Asia', 'Tehran', 'IRR', '+98'),
    ('IRQ', 'Iraq', 'Asia', 'Baghdad', 'IQD', '+964'),
    ('IRL', 'Ireland', 'Europe', 'Dublin', 'EUR', '+353'),
    ('IMN', 'Isle of Man', 'Europe', 'Douglas', 'GBP', '+44'),
    ('ISR', 'Israel', 'Asia', 'Jerusalem', 'ILS', '+972'),
    ('ITA', 'Italy', 'Europe', 'Rome', 'EUR', '+39'),
    ('JAM', 'Jamaica', 'North America', 'Kingston', 'JMD', '+1876'),
    ('JPN', 'Japan', 'Asia', 'Tokyo', 'JPY', '+81'),
    ('JEY', 'Jersey', 'Europe', 'Saint Helier', 'GBP', '+44'),
    ('JOR', 'Jordan', 'Asia', 'Amman', 'JOD', '+962'),
    ('KAZ', 'Kazakhstan', 'Asia', 'Astana', 'KZT', '+7'),
    ('KEN', 'Kenya', 'Africa', 'Nairobi', 'KES', '+254'),
    ('KIR', 'Kiribati', 'Oceania', 'Tarawa', 'AUD', '+686'),
    ('PRK', 'Korea, Democratic People''s Republic of', 'Asia', 'Pyongyang', 'KPW', '+850'),
    ('KOR', 'Korea, Republic of', 'Asia', 'Seoul', 'KRW', '+82'),
    ('KWT', 'Kuwait', 'Asia', 'Kuwait City', 'KWD', '+965'),
    ('KGZ', 'Kyrgyzstan', 'Asia', 'Bishkek', 'KGS', '+996'),
    ('LAO', 'Lao People''s Democratic Republic', 'Asia', 'Vientiane', 'LAK', '+856'),
    ('LVA', 'Latvia', 'Europe', 'Riga', 'EUR', '+371'),
    ('LBN', 'Lebanon', 'Asia', 'Beirut', 'LBP', '+961'),
    ('LSO', 'Lesotho', 'Africa', 'Maseru', 'LSL', '+266'),
    ('LBR', 'Liberia', 'Africa', 'Monrovia', 'LRD', '+231'),
    ('LBY', 'Libya', 'Africa', 'Tripoli', 'LYD', '+218'),
    ('LIE', 'Liechtenstein', 'Europe', 'Vaduz', 'CHF', '+423'),
    ('LTU', 'Lithuania', 'Europe', 'Vilnius', 'EUR', '+370'),
    ('LUX', 'Luxembourg', 'Europe', 'Luxembourg', 'EUR', '+352'),
    ('MAC', 'Macao', 'Asia', 'Macao', 'MOP', '+853'),
    ('MDG', 'Madagascar', 'Africa', 'Antananarivo', 'MGA', '+261'),
    ('MWI', 'Malawi', 'Africa', 'Lilongwe', 'MWK', '+265'),
    ('MYS', 'Malaysia', 'Asia', 'Kuala Lumpur', 'MYR', '+60'),
    ('MDV', 'Maldives', 'Asia', 'Male', 'MVR', '+960'),
    ('MLI', 'Mali', 'Africa', 'Bamako', 'XOF', '+223'),
    ('MLT', 'Malta', 'Europe', 'Valletta', 'EUR', '+356'),
    ('MHL', 'Marshall Islands', 'Oceania', 'Majuro', 'USD', '+692'),
    ('MTQ', 'Martinique', 'North America', 'Fort-de-France', 'EUR', '+596'),
    ('MRT', 'Mauritania', 'Africa', 'Nouakchott', 'MRU', '+222'),
    ('MUS', 'Mauritius', 'Africa', 'Port Louis', 'MUR', '+230'),
    ('MYT', 'Mayotte', 'Africa', 'Mamoudzou', 'EUR', '+262'),
    ('MEX', 'Mexico', 'North America', 'Mexico City', 'MXN', '+52'),
    ('FSM', 'Micronesia', 'Oceania', 'Palikir', 'USD', '+691'),
    ('MDA', 'Moldova', 'Europe', 'Chisinau', 'MDL', '+373'),
    ('MCO', 'Monaco', 'Europe', 'Monaco', 'EUR', '+377'),
    ('MNG', 'Mongolia', 'Asia', 'Ulaanbaatar', 'MNT', '+976'),
    ('MNE', 'Montenegro', 'Europe', 'Podgorica', 'EUR', '+382'),
    ('MSR', 'Montserrat', 'North America', 'Plymouth', 'XCD', '+1664'),
    ('MAR', 'Morocco', 'Africa', 'Rabat', 'MAD', '+212'),
    ('MOZ', 'Mozambique', 'Africa', 'Maputo', 'MZN', '+258'),
    ('MMR', 'Myanmar', 'Asia', 'Naypyidaw', 'MMK', '+95'),
    ('NAM', 'Namibia', 'Africa', 'Windhoek', 'NAD', '+264'),
    ('NRU', 'Nauru', 'Oceania', 'Yaren', 'AUD', '+674'),
    ('NPL', 'Nepal', 'Asia', 'Kathmandu', 'NPR', '+977'),
    ('NLD', 'Netherlands', 'Europe', 'Amsterdam', 'EUR', '+31'),
    ('NCL', 'New Caledonia', 'Oceania', 'Noumea', 'XPF', '+687'),
    ('NZL', 'New Zealand', 'Oceania', 'Wellington', 'NZD', '+64'),
    ('NIC', 'Nicaragua', 'North America', 'Managua', 'NIO', '+505'),
    ('NER', 'Niger', 'Africa', 'Niamey', 'XOF', '+227'),
    ('NGA', 'Nigeria', 'Africa', 'Abuja', 'NGN', '+234'),
    ('NIU', 'Niue', 'Oceania', 'Alofi', 'NZD', '+683'),
    ('NFK', 'Norfolk Island', 'Oceania', 'Kingston', 'AUD', '+672'),
    ('MKD', 'North Macedonia', 'Europe', 'Skopje', 'MKD', '+389'),
    ('MNP', 'Northern Mariana Islands', 'Oceania', 'Saipan', 'USD', '+1670'),
    ('NOR', 'Norway', 'Europe', 'Oslo', 'NOK', '+47'),
    ('OMN', 'Oman', 'Asia', 'Muscat', 'OMR', '+968'),
    ('PAK', 'Pakistan', 'Asia', 'Islamabad', 'PKR', '+92'),
    ('PLW', 'Palau', 'Oceania', 'Ngerulmud', 'USD', '+680'),
    ('PSE', 'Palestine, State of', 'Asia', 'Ramallah', NULL, '+970'),
    ('PAN', 'Panama', 'North America', 'Panama City', 'PAB', '+507'),
    ('PNG', 'Papua New Guinea', 'Oceania', 'Port Moresby', 'PGK', '+675'),
    ('PRY', 'Paraguay', 'South America', 'Asuncion', 'PYG', '+595'),
    ('PER', 'Peru', 'South America', 'Lima', 'PEN', '+51'),
    ('PHL', 'Philippines', 'Asia', 'Manila', 'PHP', '+63'),
    ('PCN', 'Pitcairn', 'Oceania', 'Adamstown', 'NZD', '+64'),
    ('POL', 'Poland', 'Europe', 'Warsaw', 'PLN', '+48'),
    ('PRT', 'Portugal', 'Europe', 'Lisbon', 'EUR', '+351'),
    ('PRI', 'Puerto Rico', 'North America', 'San Juan', 'USD', '+1787'),
    ('QAT', 'Qatar', 'Asia', 'Doha', 'QAR', '+974'),
    ('REU', 'Reunion', 'Africa', 'Saint-Denis', 'EUR', '+262'),
    ('ROU', 'Romania', 'Europe', 'Bucharest', 'RON', '+40'),
    ('RUS', 'Russian Federation', 'Europe', 'Moscow', 'RUB', '+7'),
    ('RWA', 'Rwanda', 'Africa', 'Kigali', 'RWF', '+250'),
    ('BLM', 'Saint Barthelemy', 'North America', 'Gustavia', 'EUR', '+590'),
    ('SHN', 'Saint Helena, Ascension and Tristan da Cunha', 'Africa', 'Jamestown', 'SHP', '+290'),
    ('KNA', 'Saint Kitts and Nevis', 'North America', 'Basseterre', 'XCD', '+1869'),
    ('LCA', 'Saint Lucia', 'North America', 'Castries', 'XCD', '+1758'),
    ('MAF', 'Saint Martin (French part)', 'North America', 'Marigot', 'EUR', '+590'),
    ('SPM', 'Saint Pierre and Miquelon', 'North America', 'Saint-Pierre', 'EUR', '+508'),
    ('VCT', 'Saint Vincent and the Grenadines', 'North America', 'Kingstown', 'XCD', '+1784'),
    ('WSM', 'Samoa', 'Oceania', 'Apia', 'WST', '+685'),
    ('SMR', 'San Marino', 'Europe', 'San Marino', 'EUR', '+378'),
    ('STP', 'Sao Tome and Principe', 'Africa', 'Sao Tome', 'STN', '+239'),
    ('SAU', 'Saudi Arabia', 'Asia', 'Riyadh', 'SAR', '+966'),
    ('SEN', 'Senegal', 'Africa', 'Dakar', 'XOF', '+221'),
    ('SRB', 'Serbia', 'Europe', 'Belgrade', 'RSD', '+381'),
    ('SYC', 'Seychelles', 'Africa', 'Victoria', 'SCR', '+248'),
    ('SLE', 'Sierra Leone', 'Africa', 'Freetown', 'SLL', '+232'),
    ('SGP', 'Singapore', 'Asia', 'Singapore', 'SGD', '+65'),
    ('SXM', 'Sint Maarten (Dutch part)', 'North America', 'Philipsburg', 'ANG', '+1721'),
    ('SVK', 'Slovakia', 'Europe', 'Bratislava', 'EUR', '+421'),
    ('SVN', 'Slovenia', 'Europe', 'Ljubljana', 'EUR', '+386'),
    ('SLB', 'Solomon Islands', 'Oceania', 'Honiara', 'SBD', '+677'),
    ('SOM', 'Somalia', 'Africa', 'Mogadishu', 'SOS', '+252'),
    ('ZAF', 'South Africa', 'Africa', 'Pretoria', 'ZAR', '+27'),
    ('SGS', 'South Georgia and the South Sandwich Islands', 'South America', 'Grytviken', 'GBP', NULL),
    ('SSD', 'South Sudan', 'Africa', 'Juba', 'SSP', '+211'),
    ('ESP', 'Spain', 'Europe', 'Madrid', 'EUR', '+34'),
    ('LKA', 'Sri Lanka', 'Asia', 'Colombo', 'LKR', '+94'),
    ('SDN', 'Sudan', 'Africa', 'Khartoum', 'SDG', '+249'),
    ('SUR', 'Suriname', 'South America', 'Paramaribo', 'SRD', '+597'),
    ('SJM', 'Svalbard and Jan Mayen', 'Europe', 'Longyearbyen', 'NOK', '+47'),
    ('SWE', 'Sweden', 'Europe', 'Stockholm', 'SEK', '+46'),
    ('CHE', 'Switzerland', 'Europe', 'Bern', 'CHF', '+41'),
    ('SYR', 'Syrian Arab Republic', 'Asia', 'Damascus', 'SYP', '+963'),
    ('TWN', 'Taiwan', 'Asia', 'Taipei', 'TWD', '+886'),
    ('TJK', 'Tajikistan', 'Asia', 'Dushanbe', 'TJS', '+992'),
    ('TZA', 'Tanzania', 'Africa', 'Dodoma', 'TZS', '+255'),
    ('THA', 'Thailand', 'Asia', 'Bangkok', 'THB', '+66'),
    ('TLS', 'Timor-Leste', 'Asia', 'Dili', 'USD', '+670'),
    ('TGO', 'Togo', 'Africa', 'Lome', 'XOF', '+228'),
    ('TKL', 'Tokelau', 'Oceania', 'Fakaofo', 'NZD', '+690'),
    ('TON', 'Tonga', 'Oceania', 'Nuku''alofa', 'TOP', '+676'),
    ('TTO', 'Trinidad and Tobago', 'North America', 'Port of Spain', 'TTD', '+1868'),
    ('TUN', 'Tunisia', 'Africa', 'Tunis', 'TND', '+216'),
    ('TUR', 'Turkey', 'Asia', 'Ankara', 'TRY', '+90'),
    ('TKM', 'Turkmenistan', 'Asia', 'Ashgabat', 'TMT', '+993'),
    ('TCA', 'Turks and Caicos Islands', 'North America', 'Cockburn Town', 'USD', '+1649'),
    ('TUV', 'Tuvalu', 'Oceania', 'Funafuti', 'AUD', '+688'),
    ('UGA', 'Uganda', 'Africa', 'Kampala', 'UGX', '+256'),
    ('UKR', 'Ukraine', 'Europe', 'Kyiv', 'UAH', '+380'),
    ('ARE', 'United Arab Emirates', 'Asia', 'Abu Dhabi', 'AED', '+971'),
    ('GBR', 'United Kingdom', 'Europe', 'London', 'GBP', '+44'),
    ('USA', 'United States', 'North America', 'Washington D.C.', 'USD', '+1'),
    ('UMI', 'United States Minor Outlying Islands', 'Oceania', NULL, 'USD', '+1'),
    ('URY', 'Uruguay', 'South America', 'Montevideo', 'UYU', '+598'),
    ('UZB', 'Uzbekistan', 'Asia', 'Tashkent', 'UZS', '+998'),
    ('VUT', 'Vanuatu', 'Oceania', 'Port Vila', 'VUV', '+678'),
    ('VEN', 'Venezuela', 'South America', 'Caracas', 'VES', '+58'),
    ('VNM', 'Viet Nam', 'Asia', 'Hanoi', 'VND', '+84'),
    ('VGB', 'Virgin Islands (British)', 'North America', 'Road Town', 'USD', '+1284'),
    ('VIR', 'Virgin Islands (U.S.)', 'North America', 'Charlotte Amalie', 'USD', '+1340'),
    ('WLF', 'Wallis and Futuna', 'Oceania', 'Mata-Utu', 'XPF', '+681'),
    ('ESH', 'Western Sahara', 'Africa', 'El Aaiun', NULL, '+212'),
    ('YEM', 'Yemen', 'Asia', 'Sana''a', 'YER', '+967'),
    ('ZMB', 'Zambia', 'Africa', 'Lusaka', 'ZMW', '+260'),
    ('ZWE', 'Zimbabwe', 'Africa', 'Harare', 'ZWL', '+263');