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
using System.IO;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/images")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ImagesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<ImageUploadResponseDto>> UploadImage(ImageUploadDto imageDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            // Check if property exists
            var property = await _unitOfWork.Properties.GetByIdAsync(imageDto.PropertyId);
            if (property == null)
                return NotFound(new { message = "العقار غير موجود" });

            var userImage = new UserUploadedImage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PropertyId = imageDto.PropertyId,
                ImageUrl = imageDto.ImageUrl,
                FileName = imageDto.FileName,
                Description = imageDto.Description,
                UploadedAt = DateTime.UtcNow,
                IsApproved = false // Requires admin approval
            };

            await _unitOfWork.Repository<UserUploadedImage>().AddAsync(userImage);
            await _unitOfWork.CompleteAsync();

            return Ok(new ImageUploadResponseDto
            {
                Id = userImage.Id,
                Success = true,
                Message = "تم رفع الصورة بنجاح وهي في انتظار الموافقة"
            });
        }

        [HttpGet("property/{propertyId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UserUploadedImageDto>>> GetPropertyImages(Guid propertyId)
        {
            var images = await _unitOfWork.Repository<UserUploadedImage>().Query()
                .Where(i => i.PropertyId == propertyId && i.IsApproved)
                .Include(i => i.User)
                .ToListAsync();

            var imageDtos = images.Select(i => new UserUploadedImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                FileName = i.FileName,
                Description = i.Description,
                UploadedAt = i.UploadedAt,
                IsApproved = i.IsApproved,
                UserName = i.User.FullName
            });

            return Ok(imageDtos);
        }

        [HttpGet("my-uploads")]
        public async Task<ActionResult<IEnumerable<UserUploadedImageDto>>> GetMyUploads()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var images = await _unitOfWork.Repository<UserUploadedImage>().Query()
                .Where(i => i.UserId == userId)
                .Include(i => i.Property)
                .ToListAsync();

            var imageDtos = images.Select(i => new UserUploadedImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                FileName = i.FileName,
                Description = i.Description,
                UploadedAt = i.UploadedAt,
                IsApproved = i.IsApproved,
                UserName = "أنت"
            });

            return Ok(imageDtos);
        }

        [HttpDelete("{imageId}")]
        public async Task<ActionResult> DeleteImage(Guid imageId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var image = await _unitOfWork.Repository<UserUploadedImage>().GetByIdAsync(imageId);

            if (image == null)
                return NotFound(new { message = "الصورة غير موجودة" });

            if (image.UserId != userId)
                return Forbid();

            _unitOfWork.Repository<UserUploadedImage>().Remove(image);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم حذف الصورة بنجاح" });
        }

        [HttpPut("{imageId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ApproveImage(Guid imageId)
        {
            var image = await _unitOfWork.Repository<UserUploadedImage>().GetByIdAsync(imageId);

            if (image == null)
                return NotFound(new { message = "الصورة غير موجودة" });

            image.IsApproved = true;
            _unitOfWork.Repository<UserUploadedImage>().Update(image);
            await _unitOfWork.CompleteAsync();

            return Ok(new { success = true, message = "تم الموافقة على الصورة" });
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserUploadedImageDto>>> GetPendingImages()
        {
            var images = await _unitOfWork.Repository<UserUploadedImage>().Query()
                .Where(i => !i.IsApproved)
                .Include(i => i.User)
                .Include(i => i.Property)
                .ToListAsync();

            var imageDtos = images.Select(i => new UserUploadedImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                FileName = i.FileName,
                Description = i.Description,
                UploadedAt = i.UploadedAt,
                IsApproved = i.IsApproved,
                UserName = i.User.FullName
            });

            return Ok(imageDtos);
        }

        [HttpGet("properties/{imageName}")]
        public IActionResult GetPropertyImage(string imageName)
        {
            try
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", imageName);
                if (!System.IO.File.Exists(imagePath))
                {
                    return NotFound("Image not found");
                }

                var fileBytes = System.IO.File.ReadAllBytes(imagePath);
                string contentType = Path.GetExtension(imageName).ToLower() switch
                {
                    ".jpg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving image", error = ex.Message });
            }
        }
    }
}
