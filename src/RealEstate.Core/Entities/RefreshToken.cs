using System;

namespace RealEstate.Core.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public required string UserId { get; set; }
        public required string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
