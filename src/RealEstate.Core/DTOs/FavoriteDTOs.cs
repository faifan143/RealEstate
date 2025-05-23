using System;

namespace RealEstate.Core.DTOs
{
    public class FavoriteDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public FavoritePropertyDto Property { get; set; } = new();
        public DateTime AddedAt { get; set; }
    }

    public class FavoritePropertyDto
    {
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public int Bedrooms { get; set; }
        public decimal Area { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }

    public class FavoriteResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
