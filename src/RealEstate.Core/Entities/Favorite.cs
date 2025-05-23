using System;

namespace RealEstate.Core.Entities
{
    public class Favorite
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public Guid PropertyId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Property Property { get; set; } = null!;
    }
}
