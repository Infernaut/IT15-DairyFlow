-- AddEquipmentCost migration
-- Run this if Supplier/RawMaterial changes are ALREADY applied to your DB.
-- If not, uncomment the block at the bottom.

BEGIN TRANSACTION;

ALTER TABLE [Equipment] ADD [Cost] decimal(18,2) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260305094938_AddEquipmentCost', N'9.0.12');

COMMIT;
GO

/*
-- Uncomment this block ONLY if your DB does NOT yet have these changes:

BEGIN TRANSACTION;

EXEC sp_rename N'[Supplier].[SupplierName]', N'Name', 'COLUMN';

ALTER TABLE [RawMaterial] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
ALTER TABLE [RawMaterial] ADD [CurrentStock] int NULL;
ALTER TABLE [RawMaterial] ADD [LastRestockDate] datetime2 NULL;
ALTER TABLE [RawMaterial] ADD [MinimumStock] int NULL;
ALTER TABLE [RawMaterial] ADD [Unit] nvarchar(50) NULL;

COMMIT;
GO
*/

