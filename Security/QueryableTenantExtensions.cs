using Microsoft.EntityFrameworkCore;

namespace IT15_DairyFlow.Security
{
    public static class QueryableTenantExtensions
    {
        /// <summary>
        /// Applies tenant filtering for entities that have a CompanyID property.
        /// Superadmin gets no company operational data by default.
        /// </summary>
        public static IQueryable<T> WhereCompany<T>(this IQueryable<T> query, ITenantContext tenant, int? companyIdOverride = null)
            where T : class
        {
            if (tenant.IsSuperAdmin)
            {
                // System-level users shouldn't see operational data unless a specific company is explicitly selected.
                if (!companyIdOverride.HasValue)
                {
                    return query.Where(_ => false);
                }
            }

            var companyId = companyIdOverride ?? tenant.CompanyId;
            if (!companyId.HasValue)
            {
                return query.Where(_ => false);
            }

            // EF.Property allows applying this convention without forcing all entities to implement an interface.
            return query.Where(e => EF.Property<int>(e, "CompanyID") == companyId.Value);
        }
    }
}
