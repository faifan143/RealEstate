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
    [Route("api/favorites")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FavoritesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<FavoriteDto>>> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var query = _unitOfWork.Favorites.Query()
                .Include(f => f.Property)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.AddedAt);

            // Get total count for pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var favorites = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to DTOs
            var favoriteDtos = _mapper.Map<IEnumerable<FavoriteDto>>(favorites);

            // Create paged result
            var result = new PagedResult<FavoriteDto>
            {
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                Items = favoriteDtos
            };

            return Ok(result);
        }

        [HttpPost("{propertyId}")]
        public async Task<ActionResult<FavoriteResponseDto>> AddToFavorites(Guid propertyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // Check if property exists
            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);

            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            // Check if already in favorites
            var existingFavorite = await _unitOfWork.Favorites.SingleOrDefaultAsync(
                f => f.UserId == userId && f.PropertyId == propertyId);

            if (existingFavorite != null)
                return BadRequest(new { message = "العقار موجود بالفعل في المفضلة" });

            // Add to favorites
            var favorite = new Favorite
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PropertyId = propertyId,
                AddedAt = DateTime.UtcNow
            };

            await _unitOfWork.Favorites.AddAsync(favorite);
            await _unitOfWork.CompleteAsync();

            return StatusCode(201, new FavoriteResponseDto
            {
                Success = true,
                Message = "تمت إضافة العقار إلى المفضلة"
            });
        }

        [HttpDelete("{propertyId}")]
        public async Task<ActionResult<FavoriteResponseDto>> RemoveFromFavorites(Guid propertyId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // Find the favorite
            var favorite = await _unitOfWork.Favorites.SingleOrDefaultAsync(
                f => f.UserId == userId && f.PropertyId == propertyId);

            if (favorite == null)
                return NotFound(new { message = "العقار غير موجود في المفضلة" });

            // Remove from favorites
            _unitOfWork.Favorites.Remove(favorite);
            await _unitOfWork.CompleteAsync();

            return Ok(new FavoriteResponseDto
            {
                Success = true,
                Message = "تمت إزالة العقار من المفضلة"
            });
        }
    }
}