namespace CarCareTracker.Models
{
    /// <summary>
    /// Response object representing server health
    /// </summary>
    public class ServerHealth
    {
        public string Status { get; set; } = "fail";
        public string Version { get; set; } = string.Empty;
        public TimeSpan TotalDuration { get; set; }
        public DatabaseHealth Database { get; set; } = new DatabaseHealth();
    }
    public class DatabaseHealth
    {
        public string Status { get; set; } = "fail";
        public string DatabaseName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}