using System;

namespace SAMS_BE.DTOs
{
    public class DocumentActionLogDto
    {
        public Guid ActionLogId { get; set; }
        public Guid DocumentId { get; set; }
        public string Action { get; set; } = null!;
        public Guid? ActorId { get; set; }
        public string? ActorName { get; set; }
        public DateTime ActionAt { get; set; }
        public string? Detail { get; set; }
    }
}





