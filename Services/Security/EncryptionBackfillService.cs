using IT15_DairyFlow.Data;
using IT15_DairyFlow.Models;
using IT15_DairyFlow.Security.Crypto;
using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Services.Security;

public interface IEncryptionBackfillService
{
    Task<EncryptionBackfillResult> BackfillAsync(int? companyId = null, CancellationToken ct = default);
}

public sealed record EncryptionBackfillResult(
    int ProductsUpdated,
    int ProductionBatchesUpdated,
    int QualityInspectionsUpdated,
    int NonConformancesUpdated,
    int EquipmentsUpdated,
    int SalesUpdated,
    int SaleTransactionsUpdated);

/// <summary>
/// One-time (or periodic) backfill tool that copies legacy plaintext columns into the new *Encrypted columns.
/// Intended for SuperAdmin use.
/// </summary>
public sealed class EncryptionBackfillService : IEncryptionBackfillService
{
    private readonly ApplicationDbContext _context;
    private readonly ICryptoService _crypto;
    private readonly ILookupHashService _lookup;

    public EncryptionBackfillService(ApplicationDbContext context, ICryptoService crypto, ILookupHashService lookup)
    {
        _context = context;
        _crypto = crypto;
        _lookup = lookup;
    }

    public async Task<EncryptionBackfillResult> BackfillAsync(int? companyId = null, CancellationToken ct = default)
    {
        // NOTE: We use EF's ExecuteUpdateAsync so we don't have to load huge tables into memory.
        // We only backfill rows where the encrypted column is null/empty and plaintext has a value.

        var productQuery = _context.Product.AsQueryable();
        if (companyId.HasValue) productQuery = productQuery.Where(p => p.CompanyID == companyId);
        var productsUpdated = await productQuery
            .Where(p => (p.ProductNameEncrypted == null || p.ProductNameEncrypted == "") && p.ProductName != null && p.ProductName != "")
            .ExecuteUpdateAsync(s =>
                s.SetProperty(p => p.ProductNameEncrypted, p => _crypto.EncryptToBase64(p.ProductName!))
                 .SetProperty(p => p.ProductNameLookupHash, p => _lookup.Compute(p.ProductName!)), ct);

        var batchQuery = _context.ProductionBatch.AsQueryable();
        if (companyId.HasValue) batchQuery = batchQuery.Where(b => b.CompanyID == companyId);
        var productionBatchesUpdated = await batchQuery
            .Where(b => (b.BatchCodeEncrypted == null || b.BatchCodeEncrypted == "") && b.BatchCode != null && b.BatchCode != "")
            .ExecuteUpdateAsync(s =>
                s.SetProperty(b => b.BatchCodeEncrypted, b => _crypto.EncryptToBase64(b.BatchCode!))
                 .SetProperty(b => b.BatchCodeLookupHash, b => _lookup.Compute(b.BatchCode!)), ct);

        var qiQuery = _context.QualityInspection.AsQueryable();
        if (companyId.HasValue) qiQuery = qiQuery.Where(q => q.CompanyID == companyId);
        var qualityInspectionsUpdated = await qiQuery
            .Where(q =>
                ((q.NotesEncrypted == null || q.NotesEncrypted == "") && q.Notes != null && q.Notes != "") ||
                ((q.CorrectiveActionEncrypted == null || q.CorrectiveActionEncrypted == "") && q.CorrectiveAction != null && q.CorrectiveAction != ""))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(q => q.NotesEncrypted, q => q.Notes == null ? q.NotesEncrypted : _crypto.EncryptToBase64(q.Notes))
                 .SetProperty(q => q.CorrectiveActionEncrypted, q => q.CorrectiveAction == null ? q.CorrectiveActionEncrypted : _crypto.EncryptToBase64(q.CorrectiveAction)), ct);

        var ncQuery = _context.NonConformance.AsQueryable();
        if (companyId.HasValue) ncQuery = ncQuery.Where(nc => nc.CompanyID == companyId);
        var nonConformancesUpdated = await ncQuery
            .Where(nc =>
                ((nc.TitleEncrypted == null || nc.TitleEncrypted == "") && nc.Title != null && nc.Title != "") ||
                ((nc.DescriptionEncrypted == null || nc.DescriptionEncrypted == "") && nc.Description != null && nc.Description != "") ||
                ((nc.RootCauseEncrypted == null || nc.RootCauseEncrypted == "") && nc.RootCause != null && nc.RootCause != "") ||
                ((nc.ImmediateActionEncrypted == null || nc.ImmediateActionEncrypted == "") && nc.ImmediateAction != null && nc.ImmediateAction != "") ||
                ((nc.CorrectiveActionEncrypted == null || nc.CorrectiveActionEncrypted == "") && nc.CorrectiveAction != null && nc.CorrectiveAction != "") ||
                ((nc.PreventiveActionEncrypted == null || nc.PreventiveActionEncrypted == "") && nc.PreventiveAction != null && nc.PreventiveAction != ""))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(nc => nc.TitleEncrypted, nc => nc.Title == null ? nc.TitleEncrypted : _crypto.EncryptToBase64(nc.Title))
                 .SetProperty(nc => nc.DescriptionEncrypted, nc => nc.Description == null ? nc.DescriptionEncrypted : _crypto.EncryptToBase64(nc.Description))
                 .SetProperty(nc => nc.RootCauseEncrypted, nc => nc.RootCause == null ? nc.RootCauseEncrypted : _crypto.EncryptToBase64(nc.RootCause))
                 .SetProperty(nc => nc.ImmediateActionEncrypted, nc => nc.ImmediateAction == null ? nc.ImmediateActionEncrypted : _crypto.EncryptToBase64(nc.ImmediateAction))
                 .SetProperty(nc => nc.CorrectiveActionEncrypted, nc => nc.CorrectiveAction == null ? nc.CorrectiveActionEncrypted : _crypto.EncryptToBase64(nc.CorrectiveAction))
                 .SetProperty(nc => nc.PreventiveActionEncrypted, nc => nc.PreventiveAction == null ? nc.PreventiveActionEncrypted : _crypto.EncryptToBase64(nc.PreventiveAction)), ct);

        var equipQuery = _context.Equipment.AsQueryable();
        if (companyId.HasValue) equipQuery = equipQuery.Where(e => e.CompanyID == companyId);
        var equipmentsUpdated = await equipQuery
            .Where(e =>
                ((e.EquipmentNameEncrypted == null || e.EquipmentNameEncrypted == "") && e.EquipmentName != null && e.EquipmentName != "") ||
                ((e.LocationEncrypted == null || e.LocationEncrypted == "") && e.Location != null && e.Location != ""))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(e => e.EquipmentNameEncrypted, e => e.EquipmentName == null ? e.EquipmentNameEncrypted : _crypto.EncryptToBase64(e.EquipmentName))
                 .SetProperty(e => e.LocationEncrypted, e => e.Location == null ? e.LocationEncrypted : _crypto.EncryptToBase64(e.Location))
                 .SetProperty(e => e.EquipmentNameLookupHash, e => e.EquipmentName == null ? e.EquipmentNameLookupHash : _lookup.Compute(e.EquipmentName)), ct);

        var saleQuery = _context.Sale.AsQueryable();
        if (companyId.HasValue) saleQuery = saleQuery.Where(s => s.CompanyID == companyId);
        var salesUpdated = await saleQuery
            .Where(s =>
                ((s.BuyerNameEncrypted == null || s.BuyerNameEncrypted == "") && s.BuyerName != null && s.BuyerName != "") ||
                ((s.BuyerEmailEncrypted == null || s.BuyerEmailEncrypted == "") && s.BuyerEmail != null && s.BuyerEmail != "") ||
                ((s.BuyerPhoneEncrypted == null || s.BuyerPhoneEncrypted == "") && s.BuyerPhone != null && s.BuyerPhone != "") ||
                ((s.NotesEncrypted == null || s.NotesEncrypted == "") && s.Notes != null && s.Notes != ""))
            .ExecuteUpdateAsync(su =>
                su.SetProperty(s => s.BuyerNameEncrypted, s => s.BuyerName == null ? s.BuyerNameEncrypted : _crypto.EncryptToBase64(s.BuyerName))
                  .SetProperty(s => s.BuyerEmailEncrypted, s => s.BuyerEmail == null ? s.BuyerEmailEncrypted : _crypto.EncryptToBase64(s.BuyerEmail))
                  .SetProperty(s => s.BuyerPhoneEncrypted, s => s.BuyerPhone == null ? s.BuyerPhoneEncrypted : _crypto.EncryptToBase64(s.BuyerPhone))
                  .SetProperty(s => s.NotesEncrypted, s => s.Notes == null ? s.NotesEncrypted : _crypto.EncryptToBase64(s.Notes))
                  .SetProperty(s => s.BuyerNameLookupHash, s => s.BuyerName == null ? s.BuyerNameLookupHash : _lookup.Compute(s.BuyerName))
                  .SetProperty(s => s.BuyerEmailLookupHash, s => s.BuyerEmail == null ? s.BuyerEmailLookupHash : _lookup.Compute(s.BuyerEmail))
                  .SetProperty(s => s.BuyerPhoneLookupHash, s => s.BuyerPhone == null ? s.BuyerPhoneLookupHash : _lookup.Compute(s.BuyerPhone)), ct);

        var txnQuery = _context.SaleTransaction.AsQueryable();
        if (companyId.HasValue) txnQuery = txnQuery.Where(t => t.CompanyID == companyId);
        var saleTransactionsUpdated = await txnQuery
            .Where(t =>
                ((t.ReferenceNumberEncrypted == null || t.ReferenceNumberEncrypted == "") && t.ReferenceNumber != null && t.ReferenceNumber != "") ||
                ((t.NotesEncrypted == null || t.NotesEncrypted == "") && t.Notes != null && t.Notes != ""))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(t => t.ReferenceNumberEncrypted, t => t.ReferenceNumber == null ? t.ReferenceNumberEncrypted : _crypto.EncryptToBase64(t.ReferenceNumber))
                 .SetProperty(t => t.NotesEncrypted, t => t.Notes == null ? t.NotesEncrypted : _crypto.EncryptToBase64(t.Notes)), ct);

        return new EncryptionBackfillResult(
            productsUpdated,
            productionBatchesUpdated,
            qualityInspectionsUpdated,
            nonConformancesUpdated,
            equipmentsUpdated,
            salesUpdated,
            saleTransactionsUpdated);
    }
}
