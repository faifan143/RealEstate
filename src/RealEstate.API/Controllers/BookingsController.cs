using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Enums;
using RealEstate.Core.Interfaces;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BookingsController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking(BookingCreateDto bookingDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var property = await _unitOfWork.Properties.GetByIdAsync(bookingDto.PropertyId);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            if (!property.IsAvailable)
                return BadRequest(new { message = "العقار غير متاح للحجز" });

            var booking = _mapper.Map<Booking>(bookingDto);
            booking.Id = Guid.NewGuid();
            booking.UserId = userId;
            booking.Status = BookingStatus.Pending;
            booking.CreatedAt = DateTime.UtcNow;
            booking.RequestDate = DateTime.SpecifyKind(bookingDto.RequestDate, DateTimeKind.Utc);
            
            if (bookingDto.VisitDateTime.HasValue)
            {
                booking.VisitDateTime = DateTime.SpecifyKind(bookingDto.VisitDateTime.Value, DateTimeKind.Utc);
            }
            
            booking.IsDirectBooking = true;

            await _unitOfWork.Repository<Booking>().AddAsync(booking);
            await _unitOfWork.CompleteAsync();

            var responseDto = _mapper.Map<BookingResponseDto>(booking);
            responseDto.Success = true;
            responseDto.ResponseMessage = "تم إنشاء طلب الحجز بنجاح";

            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, responseDto);
        }

        [HttpGet("user")]
        public async Task<ActionResult<PagedResult<BookingDto>>> GetUserBookings([FromQuery] BookingFilterParameters? parameters = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            parameters ??= new BookingFilterParameters();

            // Create a query with projection to specific columns
            var query = _unitOfWork.Repository<Booking>().Query()
                .Include(b => b.Property)
                .Where(b => b.UserId == userId);

            // Apply status filter if provided
            if (parameters.Status.HasValue)
                query = query.Where(b => b.Status == parameters.Status.Value);

            // Apply sorting (newest first)
            query = query.OrderByDescending(b => b.CreatedAt);

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Get bookings with projection to avoid missing column issue
            var bookingsData = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(b => new 
                {
                    b.Id,
                    b.PropertyId,
                    b.UserId,
                    b.Status,
                    b.RequestDate,
                    // VisitDateTime excluded to avoid potential missing column issue
                    b.Message,
                    b.ContactPhone,
                    b.CreatedAt,
                    Property = new 
                    {
                        Title = b.Property.Title,
                        Location = b.Property.Location,
                        MainImageUrl = b.Property.MainImageUrl,
                        Price = b.Property.Price
                    }
                })
                .ToListAsync();

            // Convert to BookingDto
            var bookingDtos = bookingsData.Select(b => new BookingDto
            {
                Id = b.Id,
                PropertyId = b.PropertyId,
                UserId = b.UserId,
                Status = b.Status,
                RequestDate = b.RequestDate,
                VisitDateTime = null, // Set to null since column might not exist yet
                Message = b.Message,
                ContactPhone = b.ContactPhone,
                CreatedAt = b.CreatedAt,
                IsDirectBooking = true,
                Property = new PropertyBasicInfoDto
                {
                    Title = b.Property.Title,
                    Location = b.Property.Location,
                    MainImageUrl = b.Property.MainImageUrl,
                    Price = b.Property.Price
                }
            });

            // Create paged result
            var result = new PagedResult<BookingDto>
            {
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize),
                CurrentPage = parameters.Page,
                PageSize = parameters.PageSize,
                Items = bookingDtos
            };

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // Get booking with projection to avoid missing column issue
            var bookingData = await _unitOfWork.Repository<Booking>().Query()
                .Include(b => b.Property)
                .Where(b => b.Id == id)
                .Select(b => new 
                {
                    b.Id,
                    b.PropertyId,
                    b.UserId,
                    b.Status,
                    b.RequestDate,
                    // VisitDateTime excluded to avoid potential missing column issue
                    b.Message,
                    b.ContactPhone,
                    b.CreatedAt,
                    Property = new 
                    {
                        Title = b.Property.Title,
                        Location = b.Property.Location,
                        MainImageUrl = b.Property.MainImageUrl,
                        Price = b.Property.Price
                    }
                })
                .FirstOrDefaultAsync();

            if (bookingData == null)
                return NotFound(new { message = "الحجز غير موجود" });

            // Check if the booking belongs to the user or the property owner
            if (bookingData.UserId != userId && !User.IsInRole("Admin"))
            {
                // Check if the user is the property owner
                var property = await _unitOfWork.Properties.GetByIdAsync(bookingData.PropertyId);
                if (property == null || property.OwnerId != userId)
                    return Forbid();
            }

            // Create the response DTO
            var bookingDto = new BookingResponseDto
            {
                Id = bookingData.Id,
                Status = bookingData.Status,
                RequestDate = bookingData.RequestDate,
                VisitDateTime = null, // Set to null since column might not exist yet
                Message = bookingData.Message,
                Success = true,
                ResponseMessage = "تم جلب الحجز بنجاح",
                Property = new PropertyBasicInfoDto
                {
                    Title = bookingData.Property.Title,
                    Location = bookingData.Property.Location,
                    MainImageUrl = bookingData.Property.MainImageUrl,
                    Price = bookingData.Property.Price
                }
            };

            return Ok(bookingDto);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelBooking(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // Get booking with projection first
            var bookingData = await _unitOfWork.Repository<Booking>().Query()
                .Where(b => b.Id == id)
                .Select(b => new { b.Id, b.UserId, b.Status })
                .FirstOrDefaultAsync();

            if (bookingData == null)
                return NotFound(new { message = "الحجز غير موجود" });

            if (bookingData.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (bookingData.Status != BookingStatus.Pending)
                return BadRequest(new { message = "لا يمكن إلغاء هذا الحجز في حالته الحالية" });

            // Now get the actual booking entity to update it
            var booking = await _unitOfWork.Repository<Booking>().Query()
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();

            booking.Status = BookingStatus.Canceled;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Booking>().Update(booking);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم إلغاء طلب الحجز بنجاح" });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateBookingStatus(Guid id, BookingStatusUpdateDto statusUpdateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // First get the booking data with projection
            var bookingData = await _unitOfWork.Repository<Booking>().Query()
                .Include(b => b.Property)
                .Where(b => b.Id == id)
                .Select(b => new 
                { 
                    b.Id, 
                    PropertyOwnerId = b.Property.OwnerId 
                })
                .FirstOrDefaultAsync();

            if (bookingData == null)
                return NotFound(new { message = "الحجز غير موجود" });

            // Make sure the user is the property owner or an admin
            if (bookingData.PropertyOwnerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // Now get the actual booking entity to update it
            var booking = await _unitOfWork.Repository<Booking>().Query()
                .Where(b => b.Id == id)
                .FirstOrDefaultAsync();

            booking.Status = statusUpdateDto.Status;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Booking>().Update(booking);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم تحديث حالة الحجز بنجاح" });
        }
    }
}
