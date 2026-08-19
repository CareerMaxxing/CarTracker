namespace CarCareTracker.Models
{
    public class PlanRecordInput
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ReminderRecordId { get; set; }
        public List<int> ReminderRecordIds { get; set; } = new List<int>();
        public bool HasReminder { get { return ReminderRecordId != default || ReminderRecordIds.Any(); } }
        public string DateCreated { get; set; } = DateTime.Now.ToShortDateString();
        public string DateModified { get; set; } = DateTime.Now.ToShortDateString();
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<SupplyUsage> Supplies { get; set; } = new List<SupplyUsage>();
        public ImportMode ImportMode { get; set; }
        public PlanPriority Priority { get; set; }
        public PlanProgress Progress { get; set; }
        public decimal Cost { get; set; }
        public decimal ActualCost { get; set; }
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<SupplyUsageHistory> RequisitionHistory { get; set; } = new List<SupplyUsageHistory>();
        public List<SupplyUsageHistory> DeletedRequisitionHistory { get; set; } = new List<SupplyUsageHistory>();
        public bool CopySuppliesAttachment { get; set; } = false;
        /// <summary>System-set, round-tripped like ReminderRecordId - never a direct form field. See
        /// PlanRecord.SourceMotKey.</summary>
        public string SourceMotKey { get; set; } = string.Empty;
        /// <summary>System-set (via MarkPlanRecordResolved/UnmarkPlanRecordResolved), round-tripped
        /// like ReminderRecordId/SourceMotKey - never a direct form field. Empty = not resolved. See
        /// PlanRecord.ResolvedDate.</summary>
        public string ResolvedDate { get; set; } = string.Empty;
        /// <summary>System-set (via MarkPlanRecordIgnored/UnmarkPlanRecordIgnored), round-tripped like
        /// ReminderRecordId/SourceMotKey - never a direct form field. Empty = not ignored. See
        /// PlanRecord.IgnoredDate.</summary>
        public string IgnoredDate { get; set; } = string.Empty;
        public PlanRecord ToPlanRecord() { return new PlanRecord {
            Id = Id,
            VehicleId = VehicleId,
            ReminderRecordId = ReminderRecordId,
            ReminderRecordIds = ReminderRecordIds,
            DateCreated = DateTime.Parse(DateCreated),
            DateModified = DateTime.Parse(DateModified),
            Description = Description,
            Notes = Notes,
            Files = Files,
            ImportMode = ImportMode,
            Cost = Cost,
            ActualCost = ActualCost,
            Priority = Priority,
            Progress = Progress,
            ExtraFields = ExtraFields,
            RequisitionHistory = RequisitionHistory,
            SourceMotKey = SourceMotKey,
            ResolvedDate = !string.IsNullOrWhiteSpace(ResolvedDate) && DateTime.TryParse(ResolvedDate, out var parsedResolvedDate) ? parsedResolvedDate : (DateTime?)null,
            IgnoredDate = !string.IsNullOrWhiteSpace(IgnoredDate) && DateTime.TryParse(IgnoredDate, out var parsedIgnoredDate) ? parsedIgnoredDate : (DateTime?)null
        }; }
        /// <summary>
        /// only used to hide view template button on plan create modal.
        /// </summary>
        public bool CreatedFromReminder { get; set; }
    }
}
