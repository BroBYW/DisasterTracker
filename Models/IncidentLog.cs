using SQLite;

namespace FinalAssignment.Models
{
    [Table("IncidentLogs")]
    public class IncidentLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string? IncidentId { get; set; }
        public string? DisasterType { get; set; }
        public string? LocationCoordinates { get; set; }
        public string? NetworkStatus { get; set; }

        public DateTime Timestamp { get; set; }
    }
}