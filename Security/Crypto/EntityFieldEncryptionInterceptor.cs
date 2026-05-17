using IT15_DairyFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IT15_DairyFlow.Security.Crypto
{
    /// <summary>
    /// Application-layer encryption for selected entity string fields.
    ///
    /// Design:
    /// - Plaintext properties are marked [NotMapped] and used throughout the app.
    /// - Encrypted storage properties live in the DB (e.g., ProductNameEncrypted).
    ///
    /// This interceptor:
    /// - Encrypts plaintext -> encrypted columns on SaveChanges
    /// - Decrypts encrypted columns -> plaintext on entity materialization
    ///
    /// NOTE: This keeps the app behavior "normal" but makes searching/sorting on
    /// encrypted fields impossible unless you add separate lookup hash columns.
    /// </summary>
    public sealed class EntityFieldEncryptionInterceptor : SaveChangesInterceptor, IMaterializationInterceptor
    {
        private readonly ICryptoService _crypto;
        private readonly ILookupHashService _lookup;

        public EntityFieldEncryptionInterceptor(ICryptoService crypto, ILookupHashService lookup)
        {
            _crypto = crypto;
            _lookup = lookup;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context != null)
            {
                EncryptPendingChanges(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context != null)
            {
                EncryptPendingChanges(eventData.Context);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            DecryptEntity(entity);
            return entity;
        }

        private void EncryptPendingChanges(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                EncryptEntity(entry.Entity);
            }
        }

        private void EncryptEntity(object entity)
        {
            switch (entity)
            {
                case Product p:
                    p.ProductNameEncrypted = EncryptNullable(p.ProductName);
                    p.ProductNameLookupHash = LookupNullable(p.ProductName);
                    break;

                case ProductionBatch b:
                    b.BatchCodeEncrypted = EncryptNullable(b.BatchCode);
                    b.BatchCodeLookupHash = LookupNullable(b.BatchCode);
                    break;

                case QualityInspection qi:
                    qi.NotesEncrypted = EncryptNullable(qi.Notes);
                    qi.CorrectiveActionEncrypted = EncryptNullable(qi.CorrectiveAction);
                    break;

                case NonConformance nc:
                    nc.TitleEncrypted = EncryptNullable(nc.Title);
                    nc.DescriptionEncrypted = EncryptNullable(nc.Description);
                    nc.RootCauseEncrypted = EncryptNullable(nc.RootCause);
                    nc.ImmediateActionEncrypted = EncryptNullable(nc.ImmediateAction);
                    nc.CorrectiveActionEncrypted = EncryptNullable(nc.CorrectiveAction);
                    nc.PreventiveActionEncrypted = EncryptNullable(nc.PreventiveAction);
                    break;

                case Equipment eq:
                    eq.EquipmentNameEncrypted = EncryptNullable(eq.EquipmentName);
                    eq.LocationEncrypted = EncryptNullable(eq.Location);
                    eq.EquipmentNameLookupHash = LookupNullable(eq.EquipmentName);
                    break;

                case Sale s:
                    s.BuyerNameEncrypted = EncryptNullable(s.BuyerName);
                    s.BuyerEmailEncrypted = EncryptNullable(s.BuyerEmail);
                    s.BuyerPhoneEncrypted = EncryptNullable(s.BuyerPhone);
                    s.NotesEncrypted = EncryptNullable(s.Notes);
                    s.BuyerNameLookupHash = LookupNullable(s.BuyerName);
                    s.BuyerEmailLookupHash = LookupNullable(s.BuyerEmail);
                    s.BuyerPhoneLookupHash = LookupNullable(s.BuyerPhone);
                    break;

                case SaleTransaction st:
                    st.ReferenceNumberEncrypted = EncryptNullable(st.ReferenceNumber);
                    st.NotesEncrypted = EncryptNullable(st.Notes);
                    break;
            }
        }

        private void DecryptEntity(object entity)
        {
            switch (entity)
            {
                case Product p:
                    p.ProductName = DecryptNullable(p.ProductNameEncrypted);
                    break;

                case ProductionBatch b:
                    b.BatchCode = DecryptNullable(b.BatchCodeEncrypted);
                    break;

                case QualityInspection qi:
                    qi.Notes = DecryptNullable(qi.NotesEncrypted);
                    qi.CorrectiveAction = DecryptNullable(qi.CorrectiveActionEncrypted);
                    break;

                case NonConformance nc:
                    nc.Title = DecryptNullable(nc.TitleEncrypted) ?? string.Empty;
                    nc.Description = DecryptNullable(nc.DescriptionEncrypted);
                    nc.RootCause = DecryptNullable(nc.RootCauseEncrypted);
                    nc.ImmediateAction = DecryptNullable(nc.ImmediateActionEncrypted);
                    nc.CorrectiveAction = DecryptNullable(nc.CorrectiveActionEncrypted);
                    nc.PreventiveAction = DecryptNullable(nc.PreventiveActionEncrypted);
                    break;

                case Equipment eq:
                    eq.EquipmentName = DecryptNullable(eq.EquipmentNameEncrypted) ?? string.Empty;
                    eq.Location = DecryptNullable(eq.LocationEncrypted);
                    break;

                case Sale s:
                    s.BuyerName = DecryptNullable(s.BuyerNameEncrypted) ?? string.Empty;
                    s.BuyerEmail = DecryptNullable(s.BuyerEmailEncrypted);
                    s.BuyerPhone = DecryptNullable(s.BuyerPhoneEncrypted);
                    s.Notes = DecryptNullable(s.NotesEncrypted);
                    break;

                case SaleTransaction st:
                    st.ReferenceNumber = DecryptNullable(st.ReferenceNumberEncrypted);
                    st.Notes = DecryptNullable(st.NotesEncrypted);
                    break;
            }
        }

        private string? EncryptNullable(string? plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext)) return null;
            return _crypto.EncryptToBase64(plaintext);
        }

        private string? DecryptNullable(string? ciphertext)
        {
            if (string.IsNullOrWhiteSpace(ciphertext)) return null;
            try
            {
                return _crypto.DecryptFromBase64(ciphertext);
            }
            catch
            {
                // If keys rotate or data is legacy plaintext, you may want a more controlled migration.
                return null;
            }
        }

        private string? LookupNullable(string? plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext)) return null;
            return _lookup.Compute(plaintext);
        }
    }
}
