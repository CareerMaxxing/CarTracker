namespace CarCareTracker.Models
{
    public class PlanRecord
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ReminderRecordId { get; set; }
        public List<int> ReminderRecordIds { get; set; } = new List<int>();
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public ImportMode ImportMode { get; set; }
        public PlanPriority Priority { get; set; }
        public PlanProgress Progress { get; set; }
        /// <summary>Estimated cost.</summary>
        public decimal Cost { get; set; }
        /// <summary>Actual cost, entered manually once work is costed/completed - distinct from
        /// the estimate above. See docs/execution/PHASE_06.md.</summary>
        public decimal ActualCost { get; set; }
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<SupplyUsageHistory> RequisitionHistory { get; set; } = new List<SupplyUsageHistory>();
        /// <summary>System-set link back to the MOT advisory this item was created from
        /// (StaticHelper.GetMotAdvisoryKey) - empty for ordinary Planner items. Never directly
        /// user-editable (same treatment as ReminderRecordId), only round-tripped so ordinary edits
        /// don't lose the linkage. See PHASE_17.md Increment 5.</summary>
        public string SourceMotKey { get; set; } = string.Empty;
        /// <summary>Set once the user marks an MOT-linked item resolved via the lighter status -
        /// orthogonal to Progress/PlanProgress (which stays reserved for the Idea..Done pipeline and
        /// its Kanban lanes, hardcoded as exactly 6 swimlanes) so resolving an MOT advisory never
        /// triggers Done's auto-create-a-ServiceRecord side effect. Null = not resolved. See
        /// PHASE_17.md Increment 6.</summary>
        public DateTime? ResolvedDate { get; set; }
        /// <summary>Set once the user marks an MOT-linked item as not significant enough to act on
        /// (e.g. an informational note like "engine covers fitted") - orthogonal to Progress, same
        /// treatment as ResolvedDate, but pulls the card into its own "Ignored" section of the Planner
        /// board instead of leaving it in its normal swimlane with just a badge (Resolved's treatment).
        /// Null = not ignored. See PHASE_17.md Increment 10.</summary>
        public DateTime? IgnoredDate { get; set; }
    }
}
