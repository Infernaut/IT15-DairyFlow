/*
  DairyFlow ERP - Security Schema Update
  Date: 2026-05-16

  This script updates the database to support:
   1) Enhanced AuditLog structure (replaces old Action/TimeStamp usage)
   2) Password reuse prevention via PasswordHistories
   3) Protected email storage for login via EmailLookupHash + EmailEncrypted

  Notes:
  - Run in a maintenance window.
  - Take a backup first.
  - If your DB uses a non-default schema, adjust dbo.
*/

BEGIN TRY
    BEGIN TRAN;

    /* ==========================================================
       1) AuditLog table changes
       ========================================================== */

    IF COL_LENGTH('dbo.AuditLog', 'ActionType') IS NULL
        ALTER TABLE dbo.AuditLog ADD ActionType NVARCHAR(128) NOT NULL CONSTRAINT DF_AuditLog_ActionType DEFAULT('');

    IF COL_LENGTH('dbo.AuditLog', 'Module') IS NULL
        ALTER TABLE dbo.AuditLog ADD Module NVARCHAR(128) NOT NULL CONSTRAINT DF_AuditLog_Module DEFAULT('');

    IF COL_LENGTH('dbo.AuditLog', 'EntityName') IS NULL
        ALTER TABLE dbo.AuditLog ADD EntityName NVARCHAR(128) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'EntityId') IS NULL
        ALTER TABLE dbo.AuditLog ADD EntityId NVARCHAR(128) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'Message') IS NULL
        ALTER TABLE dbo.AuditLog ADD Message NVARCHAR(2048) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'MetadataJson') IS NULL
        ALTER TABLE dbo.AuditLog ADD MetadataJson NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'IpAddress') IS NULL
        ALTER TABLE dbo.AuditLog ADD IpAddress NVARCHAR(64) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'UserAgent') IS NULL
        ALTER TABLE dbo.AuditLog ADD UserAgent NVARCHAR(256) NULL;

    IF COL_LENGTH('dbo.AuditLog', 'TimeStampUtc') IS NULL
        ALTER TABLE dbo.AuditLog ADD TimeStampUtc DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_AuditLog_TimeStampUtc DEFAULT(SYSUTCDATETIME());

    -- Backfill values from legacy columns if they exist.
    -- Important: only reference new columns like [Message] if they already exist,
    -- otherwise SQL Server can error during compilation with "Invalid column name".
    IF COL_LENGTH('dbo.AuditLog', 'Action') IS NOT NULL
       AND COL_LENGTH('dbo.AuditLog', 'Message') IS NOT NULL
    BEGIN
        -- Use dynamic SQL to avoid "Invalid column" at compile time.
        EXEC(N'
            UPDATE dbo.AuditLog
            SET Message = COALESCE(Message, [Action])
            WHERE Message IS NULL;

            UPDATE dbo.AuditLog
            SET ActionType = CASE WHEN NULLIF(ActionType, '''') IS NULL THEN ''LegacyAction'' ELSE ActionType END,
                Module = CASE WHEN NULLIF(Module, '''') IS NULL THEN ''Legacy'' ELSE Module END;
        ');
    END

    IF COL_LENGTH('dbo.AuditLog', 'TimeStamp') IS NOT NULL
       AND COL_LENGTH('dbo.AuditLog', 'TimeStampUtc') IS NOT NULL
    BEGIN
        -- Use dynamic SQL to avoid "Invalid column" at compile time.
        EXEC(N'
            UPDATE dbo.AuditLog
            SET TimeStampUtc = CASE WHEN TimeStampUtc IS NULL THEN TODATETIMEOFFSET([TimeStamp], ''+00:00'') ELSE TimeStampUtc END;
        ');
    END

    -- Drop legacy columns (optional but recommended to match code)
    IF COL_LENGTH('dbo.AuditLog', 'Action') IS NOT NULL
    BEGIN
        DECLARE @dfAction sysname;
        SELECT @dfAction = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        INNER JOIN sys.tables t
            ON t.object_id = c.object_id
        INNER JOIN sys.schemas s
            ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'AuditLog' AND c.name = 'Action';

        IF @dfAction IS NOT NULL
        BEGIN
            DECLARE @sqlDropDfAction nvarchar(max) = N'ALTER TABLE dbo.AuditLog DROP CONSTRAINT ' + QUOTENAME(@dfAction) + N';';
            EXEC(@sqlDropDfAction);
        END

        ALTER TABLE dbo.AuditLog DROP COLUMN [Action];
    END

    IF COL_LENGTH('dbo.AuditLog', 'TimeStamp') IS NOT NULL
    BEGIN
        DECLARE @dfTimeStamp sysname;
        SELECT @dfTimeStamp = dc.name
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
        INNER JOIN sys.tables t
            ON t.object_id = c.object_id
        INNER JOIN sys.schemas s
            ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'AuditLog' AND c.name = 'TimeStamp';

        IF @dfTimeStamp IS NOT NULL
        BEGIN
            DECLARE @sqlDropDfTimeStamp nvarchar(max) = N'ALTER TABLE dbo.AuditLog DROP CONSTRAINT ' + QUOTENAME(@dfTimeStamp) + N';';
            EXEC(@sqlDropDfTimeStamp);
        END

        ALTER TABLE dbo.AuditLog DROP COLUMN [TimeStamp];
    END


    /* ==========================================================
       2) PasswordHistories table
       ========================================================== */

    IF OBJECT_ID('dbo.PasswordHistories', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PasswordHistories
        (
            PasswordHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PasswordHistories PRIMARY KEY,
            UserId NVARCHAR(450) NOT NULL,
            PasswordHash NVARCHAR(2048) NOT NULL,
            CreatedUtc DATETIMEOFFSET(7) NOT NULL CONSTRAINT DF_PasswordHistories_CreatedUtc DEFAULT(SYSUTCDATETIME())
        );

        CREATE INDEX IX_PasswordHistories_UserId_CreatedUtc
            ON dbo.PasswordHistories(UserId, CreatedUtc DESC);

        ALTER TABLE dbo.PasswordHistories
            ADD CONSTRAINT FK_PasswordHistories_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id)
            ON DELETE CASCADE;
    END


    /* ==========================================================
       3) Protected email storage on AspNetUsers
       ========================================================== */

    IF COL_LENGTH('dbo.AspNetUsers', 'EmailLookupHash') IS NULL
        ALTER TABLE dbo.AspNetUsers ADD EmailLookupHash NVARCHAR(64) NULL;

    IF COL_LENGTH('dbo.AspNetUsers', 'EmailEncrypted') IS NULL
        ALTER TABLE dbo.AspNetUsers ADD EmailEncrypted NVARCHAR(MAX) NULL;

    -- Helpful index for login
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_AspNetUsers_EmailLookupHash'
          AND object_id = OBJECT_ID('dbo.AspNetUsers')
    )
    BEGIN
        CREATE INDEX IX_AspNetUsers_EmailLookupHash
            ON dbo.AspNetUsers(EmailLookupHash);
    END

    -- NOTE: EmailLookupHash/EmailEncrypted backfill cannot be done safely here
    -- without the application crypto keys. It should be done by an app tool/job.

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
