using System;
using System.Collections.Generic;
using RealEstate.Core.Enums;

namespace RealEstate.Core.Entities
{
    public class Property
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public decimal Area { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public PropertyType PropertyType { get; set; }
        public required string Location { get; set; }
        public required string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public required string MainImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Rental information
        public int? RentalDurationMonths { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public bool IsForRent { get; set; } = false;
        public bool IsForSale { get; set; } = true;

        // Owner information
        public required string OwnerId { get; set; }
        public virtual ApplicationUser Owner { get; set; } = null!;

        // Features (stored as JSON in the DB)
        public List<string> Features { get; set; } = new List<string>();

        // Navigation properties
        public virtual ICollection<PropertyImage> Images { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<UserUploadedImage> UserUploadedImages { get; set; }

        public Property()
        {
            Images = new HashSet<PropertyImage>();
            Bookings = new HashSet<Booking>();
            Favorites = new HashSet<Favorite>();
            UserUploadedImages = new HashSet<UserUploadedImage>();
        }
    }
}
