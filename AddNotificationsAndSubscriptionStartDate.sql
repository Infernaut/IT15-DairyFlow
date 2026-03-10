-- ============================================================================
-- DairyFlow ERP — New Tables & Column Definitions
-- Migrations: AddSubscriptionStartDate + AddNotifications
-- Generated: 2026-03-10
-- ============================================================================
-- This script adds:
--   1. SubscriptionStartDate column to Company table
--   2. Notification table with indexes and foreign keys
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- MIGRATION 1: AddSubscriptionStartDate
-- Adds a nullable datetime column to track when a company's subscription began
-- ────────────────────────────────────────────────────────────────────────────

BEGIN TRANSACTION;

ALTER TABLE [Company] ADD [SubscriptionStartDate] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260309182008_AddSubscriptionStartDate', N'9.0.12');

COMMIT;
GO


-- ────────────────────────────────────────────────────────────────────────────
-- MIGRATION 2: AddNotifications
-- Creates the Notification table for the in-app notification bell system
-- ────────────────────────────────────────────────────────────────────────────

BEGIN TRANSACTION;

CREATE TABLE [Notification] (
    [Id]              int            IDENTITY(1,1) NOT NULL,
    [RecipientUserId] nvarchar(450)  NOT NULL,
    [ActorUserId]     nvarchar(450)  NULL,
    [Message]         nvarchar(500)  NOT NULL,
    [Type]            nvarchar(50)   NOT NULL,
    [Icon]            nvarchar(50)   NOT NULL,
    [IsRead]          bit            NOT NULL,
    [CreatedAt]       datetime2      NOT NULL,

    CONSTRAINT [PK_Notification] PRIMARY KEY ([Id]),

    CONSTRAINT [FK_Notification_AspNetUsers_RecipientUserId]
        FOREIGN KEY ([RecipientUserId])
        REFERENCES [AspNetUsers] ([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_Notification_AspNetUsers_ActorUserId]
        FOREIGN KEY ([ActorUserId])
        REFERENCES [AspNetUsers] ([Id])
);

-- Index for fast lookup of a user's notifications by read status + date
CREATE NONCLUSTERED INDEX [IX_Notification_Recipient_Read_Date]
    ON [Notification] ([RecipientUserId], [IsRead], [CreatedAt]);

-- Index for the ActorUserId foreign key
CREATE NONCLUSTERED INDEX [IX_Notification_ActorUserId]
    ON [Notification] ([ActorUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310060753_AddNotifications', N'9.0.12');

COMMIT;
GO
