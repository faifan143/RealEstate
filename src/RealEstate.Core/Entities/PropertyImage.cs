using System;

namespace RealEstate.Core.Entities
{
    public class PropertyImage
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public required string Url { get; set; }
        public required string Description { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Property Property { get; set; } = null!;
    }
}
