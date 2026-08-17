namespace CarCareTracker.Models
{
    /// <summary>
    /// Document category (FR-DOC-01), independent of which record the file is attached to.
    /// Other = 0 so existing UploadedFiles rows (added before this field existed) deserialize to a
    /// safe default rather than an incorrect specific category.
    /// </summary>
    public enum DocumentType
    {
        Other = 0,
        Invoice = 1,
        MOT = 2,
        V5C = 3,
        Insurance = 4,
        Photograph = 5,
        Datasheet = 6
    }
}
