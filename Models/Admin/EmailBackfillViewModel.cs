namespace IT15_DairyFlow.Models.Admin
{
    public class EmailBackfillViewModel
    {
        public int TotalUsers { get; set; }
        public int UsersNeedingBackfill { get; set; }
        public int Updated { get; set; }

        public int BatchSize { get; set; } = 200;
        public bool DryRun { get; set; } = true;

        public string? Message { get; set; }
    }
}
