namespace CarCareTracker.Models
{
    /// <summary>
    /// Provenance of an odometer reading (FR-ODO-01). Deliberately separate from ImportMode - that
    /// enum drives CSV import/tabs/VisibleTabs and shouldn't gain values (like MOT) that have no
    /// record type of their own. Manual = 0 so pre-existing records without this field (added before
    /// Phase 9) deserialize to a safe, reasonable default rather than an alarming/incorrect one.
    /// </summary>
    public enum OdometerRecordSource
    {
        Manual = 0,
        ServiceRecord = 1,
        RepairRecord = 2,
        GasRecord = 3,
        UpgradeRecord = 4,
        TaxRecord = 5,
        InspectionRecord = 6,
        MOT = 7,
        Other = 8
    }
}
