using System;

namespace RealEstate.Core.Entities
{
    public class UserUploadedImage
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public Guid? PropertyId { get; set; }
        public required string ImageUrl { get; set; }
        public required string FileName { get; set; }
        public required string Description { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = false;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Property? Property { get; set; }
    }
}
