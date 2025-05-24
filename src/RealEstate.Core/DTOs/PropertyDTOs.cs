using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RealEstate.Core.Enums;

namespace RealEstate.Core.DTOs
{
    public class PropertyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Area { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public PropertyType PropertyType { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RentalDurationMonths { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public bool IsForRent { get; set; }
        public bool IsForSale { get; set; }
    }

    public class PropertyDetailsDto : PropertyDto
    {
        public string Address { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public List<PropertyImageDto> Images { get; set; } = new();
        public List<UserUploadedImageDto> UserUploadedImages { get; set; } = new();
        public PropertyOwnerDto Owner { get; set; } = new();
        public DateTime? UpdatedAt { get; set; }
    }

    public class PropertyOwnerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class PropertyCreateDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, double.MaxValue)]
        public decimal Area { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Bedrooms { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Bathrooms { get; set; }

        [Required]
        public PropertyType PropertyType { get; set; }

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public List<string> Features { get; set; } = new();

        public bool IsAvailable { get; set; } = true;

        [Required]
        public string MainImageUrl { get; set; } = string.Empty;

        public int? RentalDurationMonths { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public bool IsForRent { get; set; } = false;
        public bool IsForSale { get; set; } = true;
    }

    public class PropertyUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? Area { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public PropertyType? PropertyType { get; set; }
        public string? Location { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<string>? Features { get; set; }
        public bool? IsAvailable { get; set; }
        public int? RentalDurationMonths { get; set; }
        public DateTime? RentalEndDate { get; set; }
        public bool? IsForRent { get; set; }
        public bool? IsForSale { get; set; }
    }

    public class PropertySearchParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
        public decimal? MinPrice { get; set; } = null;
        public decimal? MaxPrice { get; set; } = null;
        public PropertyType? PropertyType { get; set; } = null;
        public int? Bedrooms { get; set; } = null;
        public string? Location { get; set; } = null;
        public string? Query { get; set; } = null;
        public bool? IsForRent { get; set; } = null;
        public bool? IsForSale { get; set; } = null;
    }

    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Items { get; set; } = new List<T>();
    }
}
