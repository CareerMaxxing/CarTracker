using System.Text.Json.Serialization;

namespace CarCareTracker.Models
{
    public class UploadedFiles
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsPending { get; set; }
        /// <summary>String-named in JSON (e.g. "Invoice") via JsonStringEnumConverter, scoped to
        /// just this property - accepts both the enum name and its numeric value on read.</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DocumentType Type { get; set; } = DocumentType.Other;
    }
}
