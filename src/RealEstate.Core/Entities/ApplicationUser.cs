using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace RealEstate.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Property> Properties { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<UserUploadedImage> UploadedImages { get; set; }

        public ApplicationUser()
        {
            Properties = new HashSet<Property>();
            Bookings = new HashSet<Booking>();
            Favorites = new HashSet<Favorite>();
            UploadedImages = new HashSet<UserUploadedImage>();
        }
    }
}
