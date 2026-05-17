using IT15_DairyFlow.Services.Security;

namespace IT15_DairyFlow.Models.Admin;

public sealed class BusinessDataEncryptionBackfillViewModel
{
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Optional scope to a single company. Null means ALL companies.
    /// </summary>
    public int? CompanyID { get; set; }

    public string? Message { get; set; }

    public EncryptionBackfillResult? Result { get; set; }
}
