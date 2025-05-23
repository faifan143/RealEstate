using System;
using System.ComponentModel.DataAnnotations;

namespace RealEstate.Core.DTOs
{
    public class ImageUploadDto
    {
        [Required]
        public Guid PropertyId { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }

    public class ImageUploadResponseDto
    {
        public Guid Id { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UserUploadedImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public bool IsApproved { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? PropertyTitle { get; set; }
    }

    public class PropertyImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
