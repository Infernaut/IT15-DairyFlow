using System.ComponentModel.DataAnnotations;

namespace IT15_DairyFlow.Models.SuperAdmin;

public sealed class SystemLogListItemViewModel
{
    public long SystemLogId { get; set; }
    public DateTimeOffset TimeStampUtc { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public int? CompanyId { get; set; }
    public string? UserId { get; set; }
    public long? DurationMs { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class SystemLogFilterViewModel
{
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }

    [MaxLength(32)]
    public string? Level { get; set; }

    [MaxLength(128)]
    public string? Component { get; set; }

    [MaxLength(128)]
    public string? EventName { get; set; }

    public int? CompanyId { get; set; }

    [MaxLength(64)]
    public string? CorrelationId { get; set; }

    public List<SystemLogListItemViewModel> Logs { get; set; } = new();
    public int TotalRecords { get; set; }
}
