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

            // Apply pagination
            var bookings = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            // Map to DTOs
            var bookingDtos = _mapper.Map<IEnumerable<BookingDto>>(bookings);

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

            var booking = await _unitOfWork.Repository<Booking>().Query()
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(new { message = "الحجز غير موجود" });

            // Check if the booking belongs to the user or the property owner
            if (booking.UserId != userId && booking.Property.OwnerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var bookingDto = _mapper.Map<BookingResponseDto>(booking);
            return Ok(bookingDto);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelBooking(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(id);

            if (booking == null)
                return NotFound(new { message = "الحجز غير موجود" });

            if (booking.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            if (booking.Status != BookingStatus.Pending)
                return BadRequest(new { message = "لا يمكن إلغاء هذا الحجز في حالته الحالية" });

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

            var booking = await _unitOfWork.Repository<Booking>().Query()
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound(new { message = "الحجز غير موجود" });

            // Make sure the user is the property owner or an admin
            if (booking.Property.OwnerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            booking.Status = statusUpdateDto.Status;
            booking.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Booking>().Update(booking);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم تحديث حالة الحجز بنجاح" });
        }
    }
}
