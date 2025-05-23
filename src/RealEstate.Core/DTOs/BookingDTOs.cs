using System;
using System.ComponentModel.DataAnnotations;
using RealEstate.Core.Enums;

namespace RealEstate.Core.DTOs
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyLocation { get; set; } = string.Empty;
        public BookingStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? VisitDateTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDirectBooking { get; set; }
    }

    public class BookingCreateDto
    {
        [Required]
        public Guid PropertyId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public DateTime? VisitDateTime { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string ContactPhone { get; set; } = string.Empty;
    }

    public class BookingUpdateDto
    {
        public BookingStatus? Status { get; set; }
        public DateTime? VisitDateTime { get; set; }
        public string? Message { get; set; }
        public string? ContactPhone { get; set; }
    }

    public class BookingResponseDto
    {
        public Guid Id { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? VisitDateTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ResponseMessage { get; set; } = string.Empty;
    }

    public class BookingFilterParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public BookingStatus? Status { get; set; }
    }

    public class BookingStatusUpdateDto
    {
        [Required]
        public BookingStatus Status { get; set; }
    }
}
