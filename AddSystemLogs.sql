-- Creates a dedicated SystemLogs table for operational/technical logging.
-- This is intentionally separate from AuditLog(s), which tracks user actions.
-- Target: SQL Server

IF OBJECT_ID(N'dbo.SystemLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemLogs
    (
        SystemLogId   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemLogs PRIMARY KEY,
        TimeStampUtc  DATETIMEOFFSET(7)    NOT NULL CONSTRAINT DF_SystemLogs_TimeStampUtc DEFAULT (SYSUTCDATETIME()),
        [Level]       NVARCHAR(32)         NOT NULL CONSTRAINT DF_SystemLogs_Level DEFAULT (N'Information'),
        Component     NVARCHAR(128)        NOT NULL CONSTRAINT DF_SystemLogs_Component DEFAULT (N''),
        EventName     NVARCHAR(128)        NOT NULL CONSTRAINT DF_SystemLogs_EventName DEFAULT (N''),
        [Message]     NVARCHAR(2048)       NULL,
        CompanyId     INT                  NULL,
        UserId        NVARCHAR(450)        NULL,
        CorrelationId NVARCHAR(64)         NULL,
        IpAddress     NVARCHAR(64)         NULL,
        UserAgent     NVARCHAR(256)        NULL,
        DurationMs    BIGINT               NULL,
        MetadataJson  NVARCHAR(MAX)        NULL
    );

    -- Useful indexes for filtering in SuperAdmin
    CREATE INDEX IX_SystemLogs_TimeStampUtc ON dbo.SystemLogs (TimeStampUtc);
    CREATE INDEX IX_SystemLogs_Level_Component_Event ON dbo.SystemLogs ([Level], Component, EventName);
    CREATE INDEX IX_SystemLogs_CompanyId ON dbo.SystemLogs (CompanyId);
END
ELSE
BEGIN
    PRINT 'dbo.SystemLogs already exists; skipping create.';
END
