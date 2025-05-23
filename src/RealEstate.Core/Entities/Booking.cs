using System;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public required string UserId { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public DateTime RequestDate { get; set; }
        public required string Message { get; set; }
        public required string ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Direct booking without queue system
        public DateTime? VisitDateTime { get; set; }
        public bool IsDirectBooking { get; set; } = true;

        // Navigation properties
        public virtual Property Property { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
