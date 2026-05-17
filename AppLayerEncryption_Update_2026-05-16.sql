/*
  DairyFlow ERP - App-Layer Encryption Schema Update
  Date: 2026-05-16

  Adds encrypted-at-rest columns used by EntityFieldEncryptionInterceptor.

  IMPORTANT:
  - This is APP-LAYER encryption (AES-GCM via CryptoService).
  - The application reads/writes plaintext properties but stores ciphertext in *Encrypted columns.
    - Searching/sorting on encrypted columns is not supported unless you add separate lookup hash columns.
        This script now also adds *LookupHash columns (Base64 HMAC-SHA256) + indexes for equality search.

  Run this script BEFORE enabling the feature in production.
  Always backup first.
*/

BEGIN TRY
    BEGIN TRAN;

    /* =====================
       PLM / Product
       ===================== */
    IF COL_LENGTH('dbo.Product', 'ProductNameEncrypted') IS NULL
        ALTER TABLE dbo.Product ADD ProductNameEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Product', 'ProductNameLookupHash') IS NULL
        ALTER TABLE dbo.Product ADD ProductNameLookupHash NVARCHAR(64) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_CompanyID_ProductNameLookupHash' AND object_id = OBJECT_ID('dbo.Product'))
        CREATE INDEX IX_Product_CompanyID_ProductNameLookupHash ON dbo.Product(CompanyID, ProductNameLookupHash);

    /* =====================
       Production / Batches
       ===================== */
    IF COL_LENGTH('dbo.ProductionBatch', 'BatchCodeEncrypted') IS NULL
        ALTER TABLE dbo.ProductionBatch ADD BatchCodeEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.ProductionBatch', 'BatchCodeLookupHash') IS NULL
        ALTER TABLE dbo.ProductionBatch ADD BatchCodeLookupHash NVARCHAR(64) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ProductionBatch_CompanyID_BatchCodeLookupHash' AND object_id = OBJECT_ID('dbo.ProductionBatch'))
        CREATE INDEX IX_ProductionBatch_CompanyID_BatchCodeLookupHash ON dbo.ProductionBatch(CompanyID, BatchCodeLookupHash);

    /* =====================
       Quality / Inspections
       ===================== */
    IF COL_LENGTH('dbo.QualityInspection', 'NotesEncrypted') IS NULL
        ALTER TABLE dbo.QualityInspection ADD NotesEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.QualityInspection', 'CorrectiveActionEncrypted') IS NULL
        ALTER TABLE dbo.QualityInspection ADD CorrectiveActionEncrypted NVARCHAR(MAX) NULL;

    /* =====================
       Quality / NCR
       ===================== */
    IF COL_LENGTH('dbo.NonConformance', 'TitleEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD TitleEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.NonConformance', 'DescriptionEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD DescriptionEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.NonConformance', 'RootCauseEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD RootCauseEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.NonConformance', 'ImmediateActionEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD ImmediateActionEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.NonConformance', 'CorrectiveActionEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD CorrectiveActionEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.NonConformance', 'PreventiveActionEncrypted') IS NULL
        ALTER TABLE dbo.NonConformance ADD PreventiveActionEncrypted NVARCHAR(MAX) NULL;

    /* =====================
       Equipment
       ===================== */
    IF COL_LENGTH('dbo.Equipment', 'EquipmentNameEncrypted') IS NULL
        ALTER TABLE dbo.Equipment ADD EquipmentNameEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Equipment', 'EquipmentNameLookupHash') IS NULL
        ALTER TABLE dbo.Equipment ADD EquipmentNameLookupHash NVARCHAR(64) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Equipment_CompanyID_EquipmentNameLookupHash' AND object_id = OBJECT_ID('dbo.Equipment'))
        CREATE INDEX IX_Equipment_CompanyID_EquipmentNameLookupHash ON dbo.Equipment(CompanyID, EquipmentNameLookupHash);

    IF COL_LENGTH('dbo.Equipment', 'LocationEncrypted') IS NULL
        ALTER TABLE dbo.Equipment ADD LocationEncrypted NVARCHAR(MAX) NULL;

    /* =====================
       Sales
       ===================== */
    IF COL_LENGTH('dbo.Sale', 'BuyerNameEncrypted') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerNameEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Sale', 'BuyerNameLookupHash') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerNameLookupHash NVARCHAR(64) NULL;

    IF COL_LENGTH('dbo.Sale', 'BuyerEmailEncrypted') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerEmailEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Sale', 'BuyerEmailLookupHash') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerEmailLookupHash NVARCHAR(64) NULL;

    IF COL_LENGTH('dbo.Sale', 'BuyerPhoneEncrypted') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerPhoneEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.Sale', 'BuyerPhoneLookupHash') IS NULL
        ALTER TABLE dbo.Sale ADD BuyerPhoneLookupHash NVARCHAR(64) NULL;

    IF COL_LENGTH('dbo.Sale', 'NotesEncrypted') IS NULL
        ALTER TABLE dbo.Sale ADD NotesEncrypted NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sale_CompanyID_BuyerEmailLookupHash' AND object_id = OBJECT_ID('dbo.Sale'))
        CREATE INDEX IX_Sale_CompanyID_BuyerEmailLookupHash ON dbo.Sale(CompanyID, BuyerEmailLookupHash);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sale_CompanyID_BuyerNameLookupHash' AND object_id = OBJECT_ID('dbo.Sale'))
        CREATE INDEX IX_Sale_CompanyID_BuyerNameLookupHash ON dbo.Sale(CompanyID, BuyerNameLookupHash);

    IF COL_LENGTH('dbo.SaleTransaction', 'ReferenceNumberEncrypted') IS NULL
        ALTER TABLE dbo.SaleTransaction ADD ReferenceNumberEncrypted NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.SaleTransaction', 'NotesEncrypted') IS NULL
        ALTER TABLE dbo.SaleTransaction ADD NotesEncrypted NVARCHAR(MAX) NULL;

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
