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
using RealEstate.Core.Interfaces;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/properties")]
    public class PropertiesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PropertiesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<PropertyDto>>> GetProperties([FromQuery] PropertySearchParameters? parameters = null)
        {
            parameters ??= new PropertySearchParameters();

            var query = _unitOfWork.Properties.Query();

            // Apply filters
            if (parameters.MinPrice.HasValue)
                query = query.Where(p => p.Price >= parameters.MinPrice.Value);

            if (parameters.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= parameters.MaxPrice.Value);

            if (parameters.PropertyType.HasValue)
                query = query.Where(p => p.PropertyType == parameters.PropertyType.Value);

            if (parameters.Bedrooms.HasValue)
                query = query.Where(p => p.Bedrooms == parameters.Bedrooms.Value);

            if (!string.IsNullOrEmpty(parameters.Location))
                query = query.Where(p => p.Location.Contains(parameters.Location));

            if (parameters.IsForRent.HasValue)
                query = query.Where(p => p.IsForRent == parameters.IsForRent.Value);

            if (parameters.IsForSale.HasValue)
                query = query.Where(p => p.IsForSale == parameters.IsForSale.Value);

            if (!string.IsNullOrEmpty(parameters.Query))
            {
                query = query.Where(p =>
                    p.Title.Contains(parameters.Query) ||
                    p.Description.Contains(parameters.Query) ||
                    p.Location.Contains(parameters.Query));
            }

            string sortBy = !string.IsNullOrEmpty(parameters.SortBy) ? parameters.SortBy : "CreatedAt";
            bool isAscending = !string.IsNullOrEmpty(parameters.SortDirection) &&
                               parameters.SortDirection.ToLower() == "asc";

            query = ApplySorting(query, sortBy, isAscending);

            var totalCount = await query.CountAsync();

            int page = parameters.Page > 0 ? parameters.Page : 1;
            int pageSize = parameters.PageSize > 0 ? parameters.PageSize : 10;

            var properties = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var propertyDtos = _mapper.Map<IEnumerable<PropertyDto>>(properties);

            var result = new PagedResult<PropertyDto>
            {
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                Items = propertyDtos
            };

            return Ok(result);
        }

        private IQueryable<Property> ApplySorting(IQueryable<Property> query, string sortBy, bool ascending)
        {
            return sortBy.ToLower() switch
            {
                "price" => ascending ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                "date" or "createdat" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                "area" => ascending ? query.OrderBy(p => p.Area) : query.OrderByDescending(p => p.Area),
                _ => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt)
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDetailsDto>> GetProperty(Guid id)
        {
            var property = await _unitOfWork.Properties.Query()
                .Include(p => p.Images)
                .Include(p => p.Owner)
                .Include(p => p.UserUploadedImages.Where(i => i.IsApproved))
                .ThenInclude(i => i.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            var propertyDto = _mapper.Map<PropertyDetailsDto>(property);

            return Ok(propertyDto);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PropertyDto>> CreateProperty(PropertyCreateDto propertyDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var property = _mapper.Map<Property>(propertyDto);
            property.Id = Guid.NewGuid();
            property.OwnerId = userId;
            property.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Properties.AddAsync(property);
            await _unitOfWork.CompleteAsync();

            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, _mapper.Map<PropertyDto>(property));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateProperty(Guid id, PropertyUpdateDto propertyDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var property = await _unitOfWork.Properties.GetByIdAsync(id);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            if (property.OwnerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // Update fields
            if (propertyDto.Title != null) property.Title = propertyDto.Title;
            if (propertyDto.Description != null) property.Description = propertyDto.Description;
            if (propertyDto.Address != null) property.Address = propertyDto.Address;
            if (propertyDto.Location != null) property.Location = propertyDto.Location;
            if (propertyDto.Price.HasValue) property.Price = propertyDto.Price.Value;
            if (propertyDto.Area.HasValue) property.Area = propertyDto.Area.Value;
            if (propertyDto.Bedrooms.HasValue) property.Bedrooms = propertyDto.Bedrooms.Value;
            if (propertyDto.Bathrooms.HasValue) property.Bathrooms = propertyDto.Bathrooms.Value;
            if (propertyDto.PropertyType.HasValue) property.PropertyType = propertyDto.PropertyType.Value;
            if (propertyDto.Latitude.HasValue) property.Latitude = propertyDto.Latitude.Value;
            if (propertyDto.Longitude.HasValue) property.Longitude = propertyDto.Longitude.Value;
            if (propertyDto.Features != null) property.Features = propertyDto.Features;
            if (propertyDto.IsAvailable.HasValue) property.IsAvailable = propertyDto.IsAvailable.Value;
            if (propertyDto.RentalDurationMonths.HasValue) property.RentalDurationMonths = propertyDto.RentalDurationMonths.Value;
            if (propertyDto.RentalEndDate.HasValue) property.RentalEndDate = propertyDto.RentalEndDate.Value;
            if (propertyDto.IsForRent.HasValue) property.IsForRent = propertyDto.IsForRent.Value;
            if (propertyDto.IsForSale.HasValue) property.IsForSale = propertyDto.IsForSale.Value;

            property.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CompleteAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteProperty(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var property = await _unitOfWork.Properties.GetByIdAsync(id);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            if (property.OwnerId != userId && !User.IsInRole("Admin"))
                return Forbid();

            _unitOfWork.Properties.Remove(property);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم حذف العقار بنجاح" });
        }
    }
}
