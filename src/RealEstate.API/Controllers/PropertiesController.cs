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
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

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
            try
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

                // Handle rental properties filtering without using direct column reference
                if (parameters.IsForRent.HasValue || parameters.IsForSale.HasValue)
                {
                    // Filter based on rental duration or end date instead
                    if (parameters.IsForRent.HasValue && parameters.IsForRent.Value)
                    {
                        query = query.Where(p => p.RentalDurationMonths != null || p.RentalEndDate != null);
                    }
                    else if (parameters.IsForSale.HasValue && parameters.IsForSale.Value)
                    {
                        query = query.Where(p => p.RentalDurationMonths == null && p.RentalEndDate == null);
                    }
                }

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

                // Select only the columns that exist in the database
                var propertiesFromDb = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Description,
                        p.Price,
                        p.Area,
                        p.Bedrooms,
                        p.Bathrooms,
                        p.PropertyType,
                        p.Location,
                        p.Address,
                        p.Latitude,
                        p.Longitude,
                        p.MainImageUrl,
                        p.IsAvailable,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.RentalDurationMonths,
                        p.RentalEndDate,
                        p.OwnerId,
                        p.Features
                    })
                    .ToListAsync();

                // Convert to Property objects with manual mapping
                var properties = propertiesFromDb.Select(p => new Property
                {
                    Id = p.Id,
                    Title = p.Title ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    Area = p.Area,
                    Bedrooms = p.Bedrooms,
                    Bathrooms = p.Bathrooms,
                    PropertyType = p.PropertyType,
                    Location = p.Location ?? string.Empty,
                    Address = p.Address ?? string.Empty,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    MainImageUrl = p.MainImageUrl ?? string.Empty,
                    IsAvailable = p.IsAvailable,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    RentalDurationMonths = p.RentalDurationMonths,
                    RentalEndDate = p.RentalEndDate,
                    OwnerId = p.OwnerId ?? string.Empty,
                    Features = p.Features ?? new List<string>()
                }).ToList();

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
            catch (Exception ex) when (ex.Message.Contains("column") && ex.Message.Contains("does not exist"))
            {
                // Fallback approach if the columns don't exist yet
                parameters ??= new PropertySearchParameters();

                var query = _unitOfWork.Properties.Query();

                // Basic filters without the problematic columns
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

                // Get all properties with a simpler select that doesn't include problematic columns
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
            try
            {
                // Get basic property info first with a specific select to avoid missing columns
                var propertyBasic = await _unitOfWork.Properties.Query()
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Description,
                        p.Price,
                        p.Area,
                        p.Bedrooms,
                        p.Bathrooms,
                        p.PropertyType,
                        p.Location,
                        p.Address,
                        p.Latitude,
                        p.Longitude,
                        p.MainImageUrl,
                        p.IsAvailable,
                        p.CreatedAt,
                        p.UpdatedAt,
                        p.RentalDurationMonths,
                        p.RentalEndDate,
                        p.OwnerId,
                        p.Features
                    })
                    .FirstOrDefaultAsync();

                if (propertyBasic == null)
                    return NotFound(new { message = "العقار غير موجود" });

                // Get related data
                var owner = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(propertyBasic.OwnerId);
                var images = await _unitOfWork.PropertyImages.Query()
                    .Where(i => i.PropertyId == id)
                    .ToListAsync() ?? new List<PropertyImage>();
                var userImages = await _unitOfWork.Repository<UserUploadedImage>().Query()
                    .Where(i => i.PropertyId == id && i.IsApproved)
                    .Include(i => i.User)
                    .ToListAsync() ?? new List<UserUploadedImage>();

                // Create full property object
                var property = new Property
                {
                    Id = propertyBasic.Id,
                    Title = propertyBasic.Title,
                    Description = propertyBasic.Description,
                    Price = propertyBasic.Price,
                    Area = propertyBasic.Area,
                    Bedrooms = propertyBasic.Bedrooms,
                    Bathrooms = propertyBasic.Bathrooms,
                    PropertyType = propertyBasic.PropertyType,
                    Location = propertyBasic.Location,
                    Address = propertyBasic.Address,
                    Latitude = propertyBasic.Latitude,
                    Longitude = propertyBasic.Longitude,
                    MainImageUrl = propertyBasic.MainImageUrl,
                    IsAvailable = propertyBasic.IsAvailable,
                    CreatedAt = propertyBasic.CreatedAt,
                    UpdatedAt = propertyBasic.UpdatedAt,
                    RentalDurationMonths = propertyBasic.RentalDurationMonths,
                    RentalEndDate = propertyBasic.RentalEndDate,
                    OwnerId = propertyBasic.OwnerId,
                    Features = propertyBasic.Features,
                    Owner = owner ?? new ApplicationUser { FullName = "Unknown" },
                    Images = images,
                    UserUploadedImages = userImages
                };

                var propertyDto = _mapper.Map<PropertyDetailsDto>(property);

                return Ok(propertyDto);
            }
            catch (Exception ex) when (ex.Message.Contains("column") && ex.Message.Contains("does not exist"))
            {
                // Fallback approach if the columns don't exist yet
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                
                if (property == null)
                    return NotFound(new { message = "العقار غير موجود" });

                // Get related data
                var owner = await _unitOfWork.Repository<ApplicationUser>().GetByIdAsync(property.OwnerId);
                var images = await _unitOfWork.PropertyImages.Query()
                    .Where(i => i.PropertyId == id)
                    .ToListAsync() ?? new List<PropertyImage>();
                var userImages = await _unitOfWork.Repository<UserUploadedImage>().Query()
                    .Where(i => i.PropertyId == id && i.IsApproved)
                    .Include(i => i.User)
                    .ToListAsync() ?? new List<UserUploadedImage>();

                // Add related data
                property.Owner = owner ?? new ApplicationUser { FullName = "Unknown" };
                property.Images = images;
                property.UserUploadedImages = userImages;

                var propertyDto = _mapper.Map<PropertyDetailsDto>(property);

                return Ok(propertyDto);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PropertyDto>> CreateProperty(PropertyCreateDto propertyDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var property = new Property
            {
                Id = Guid.NewGuid(),
                Title = propertyDto.Title,
                Description = propertyDto.Description,
                Price = propertyDto.Price,
                Area = propertyDto.Area,
                Bedrooms = propertyDto.Bedrooms,
                Bathrooms = propertyDto.Bathrooms,
                PropertyType = propertyDto.PropertyType,
                Location = propertyDto.Location,
                Address = propertyDto.Address,
                Latitude = propertyDto.Latitude,
                Longitude = propertyDto.Longitude,
                MainImageUrl = propertyDto.MainImageUrl,
                IsAvailable = propertyDto.IsAvailable,
                OwnerId = userId,
                Features = propertyDto.Features,
                CreatedAt = DateTime.UtcNow,
                // Set rental info based on IsForRent flag from DTO
                RentalDurationMonths = propertyDto.IsForRent ? propertyDto.RentalDurationMonths : null,
                RentalEndDate = propertyDto.IsForRent && propertyDto.RentalEndDate.HasValue ? 
                    DateTime.SpecifyKind(propertyDto.RentalEndDate.Value, DateTimeKind.Utc) : null,
                IsForRent = propertyDto.IsForRent,
                IsForSale = propertyDto.IsForSale
            };

            await _unitOfWork.Properties.AddAsync(property);
            await _unitOfWork.CompleteAsync();

            var resultDto = _mapper.Map<PropertyDto>(property);
            return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, resultDto);
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
            
            // Update rental information based on IsForRent flag
            if (propertyDto.IsForRent.HasValue)
            {
                if (propertyDto.IsForRent.Value)
                {
                    // If property is for rent, set rental values from the DTO
                    property.RentalDurationMonths = propertyDto.RentalDurationMonths ?? property.RentalDurationMonths;
                    property.RentalEndDate = propertyDto.RentalEndDate ?? property.RentalEndDate;
                }
                else
                {
                    // If property is not for rent, clear rental values
                    property.RentalDurationMonths = null;
                    property.RentalEndDate = null;
                }
            }
            else
            {
                // Update individual rental fields if provided
                if (propertyDto.RentalDurationMonths.HasValue) property.RentalDurationMonths = propertyDto.RentalDurationMonths.Value;
                if (propertyDto.RentalEndDate.HasValue) property.RentalEndDate = DateTime.SpecifyKind(propertyDto.RentalEndDate.Value, DateTimeKind.Utc);
            }

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

        [HttpPost("with-images")]
        [Authorize]
        public async Task<ActionResult<PropertyDto>> CreatePropertyWithImages([FromForm] PropertyCreateFormDto formData)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "المستخدم غير مصرح له" });

                // Parse the property data from form
                PropertyCreateDto propertyDto;
                if (formData.PropertyData != null)
                {
                    propertyDto = JsonSerializer.Deserialize<PropertyCreateDto>(formData.PropertyData, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    // Fallback to direct form mapping
                    propertyDto = new PropertyCreateDto
                    {
                        Title = formData.Title,
                        Description = formData.Description,
                        Price = formData.Price,
                        Area = formData.Area,
                        Bedrooms = formData.Bedrooms,
                        Bathrooms = formData.Bathrooms,
                        PropertyType = formData.PropertyType,
                        Location = formData.Location,
                        Address = formData.Address,
                        Latitude = formData.Latitude,
                        Longitude = formData.Longitude,
                        IsAvailable = formData.IsAvailable,
                        IsForRent = formData.IsForRent,
                        IsForSale = formData.IsForSale,
                        RentalDurationMonths = formData.RentalDurationMonths,
                        RentalEndDate = formData.RentalEndDate,
                        Features = formData.Features?.Split(',').ToList() ?? new List<string>()
                    };
                }

                // Create property
                var property = new Property
                {
                    Id = Guid.NewGuid(),
                    Title = propertyDto.Title,
                    Description = propertyDto.Description,
                    Price = propertyDto.Price,
                    Area = propertyDto.Area,
                    Bedrooms = propertyDto.Bedrooms,
                    Bathrooms = propertyDto.Bathrooms,
                    PropertyType = propertyDto.PropertyType,
                    Location = propertyDto.Location,
                    Address = propertyDto.Address,
                    Latitude = propertyDto.Latitude,
                    Longitude = propertyDto.Longitude,
                    IsAvailable = propertyDto.IsAvailable,
                    OwnerId = userId,
                    Features = propertyDto.Features,
                    CreatedAt = DateTime.UtcNow,
                    MainImageUrl = propertyDto.MainImageUrl ?? "/images/properties/default.jpg", // Default image if none provided
                    // Set rental info based on IsForRent flag from DTO
                    RentalDurationMonths = propertyDto.IsForRent ? propertyDto.RentalDurationMonths : null,
                    RentalEndDate = propertyDto.IsForRent && propertyDto.RentalEndDate.HasValue ? 
                        DateTime.SpecifyKind(propertyDto.RentalEndDate.Value, DateTimeKind.Utc) : null,
                    IsForRent = propertyDto.IsForRent,
                    IsForSale = propertyDto.IsForSale
                };

                // Handle main image upload
                if (formData.MainImage != null)
                {
                    string mainImageUrl = await SaveImageAsync(formData.MainImage);
                    property.MainImageUrl = mainImageUrl;
                }
                else
                {
                    property.MainImageUrl = propertyDto.MainImageUrl;
                }

                // Save property
                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.CompleteAsync();

                // Handle additional images
                if (formData.AdditionalImages != null && formData.AdditionalImages.Count > 0)
                {
                    foreach (var image in formData.AdditionalImages)
                    {
                        string imageUrl = await SaveImageAsync(image);
                        
                        var propertyImage = new PropertyImage
                        {
                            Id = Guid.NewGuid(),
                            PropertyId = property.Id,
                            Url = imageUrl,
                            Description = "Additional image for " + property.Title,
                            Order = 0,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.PropertyImages.AddAsync(propertyImage);
                    }

                    await _unitOfWork.CompleteAsync();
                }

                // Get the complete property with image URLs
                var resultDto = _mapper.Map<PropertyDto>(property);
                return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, resultDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء إنشاء العقار", error = ex.Message });
            }
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            try
            {
                // Use forward slashes for cross-platform compatibility
                var folderName = Path.Combine("wwwroot", "images", "properties");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                
                // Ensure directory exists with proper permissions
                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                    
                    // Set permissions for Linux containers
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        try
                        {
                            var directoryInfo = new DirectoryInfo(pathToSave);
                            directoryInfo.Attributes &= ~FileAttributes.ReadOnly;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not set directory permissions: {ex.Message}");
                        }
                    }
                }

                // Create a unique filename with sanitization
                string originalFileName = ContentDispositionHeaderValue.Parse(image.ContentDisposition).FileName?.Trim('"') ?? "image";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                string extension = Path.GetExtension(originalFileName);
                
                // Sanitize filename for cross-platform compatibility
                fileNameWithoutExtension = System.Text.RegularExpressions.Regex.Replace(fileNameWithoutExtension, @"[^a-zA-Z0-9_-]", "_");
                
                string uniqueFileName = $"{fileNameWithoutExtension}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                string fullPath = Path.Combine(pathToSave, uniqueFileName);
                
                // Copy file with proper error handling
                using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await image.CopyToAsync(stream);
                    await stream.FlushAsync();
                }
                
                // Verify file was created
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException("Failed to save image file");
                }
                
                // Set file permissions for Linux containers
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    try
                    {
                        var fileInfo = new FileInfo(fullPath);
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not set file permissions: {ex.Message}");
                    }
                }

                // Return URL with forward slashes for web compatibility
                return $"/images/properties/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image: {ex.Message}");
                throw new InvalidOperationException($"Failed to save image: {ex.Message}", ex);
            }
        }

        [HttpPost("{id}/images")]
        [Authorize]
        public async Task<ActionResult> AddPropertyImages(Guid id, [FromForm] List<IFormFile> images)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "المستخدم غير مصرح له" });

                var property = await _unitOfWork.Properties.GetByIdAsync(id);

                if (property == null)
                    return NotFound(new { message = "العقار غير موجود" });

                if (property.OwnerId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                if (images == null || !images.Any())
                    return BadRequest(new { message = "لم يتم تقديم أي صور" });

                foreach (var image in images)
                {
                    string imageUrl = await SaveImageAsync(image);
                    
                    var propertyImage = new PropertyImage
                    {
                        Id = Guid.NewGuid(),
                        PropertyId = property.Id,
                        Url = imageUrl,
                        Description = "Additional image for " + property.Title,
                        Order = 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.PropertyImages.AddAsync(propertyImage);
                }

                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "تمت إضافة الصور بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء إضافة الصور", error = ex.Message });
            }
        }

        [HttpPut("{id}/main-image")]
        [Authorize]
        public async Task<ActionResult> UpdateMainImage(Guid id, [FromForm] IFormFile mainImage)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "المستخدم غير مصرح له" });

                var property = await _unitOfWork.Properties.GetByIdAsync(id);

                if (property == null)
                    return NotFound(new { message = "العقار غير موجود" });

                if (property.OwnerId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                if (mainImage == null)
                    return BadRequest(new { message = "لم يتم تقديم صورة" });

                string imageUrl = await SaveImageAsync(mainImage);
                property.MainImageUrl = imageUrl;
                property.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Properties.Update(property);
                await _unitOfWork.CompleteAsync();

                return Ok(new { message = "تم تحديث الصورة الرئيسية بنجاح", imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء تحديث الصورة الرئيسية", error = ex.Message });
            }
        }

        [HttpDelete("images/{imageId}")]
        [Authorize]
        public async Task<ActionResult> DeletePropertyImage(Guid imageId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "المستخدم غير مصرح له" });

                var image = await _unitOfWork.PropertyImages.Query()
                    .FirstOrDefaultAsync(i => i.Id == imageId);

                if (image == null)
                    return NotFound(new { message = "الصورة غير موجودة" });

                var property = await _unitOfWork.Properties.GetByIdAsync(image.PropertyId);

                if (property == null)
                    return NotFound(new { message = "العقار غير موجود" });

                if (property.OwnerId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                _unitOfWork.PropertyImages.Remove(image);
                await _unitOfWork.CompleteAsync();

                // Optionally delete the physical file
                // DeleteImageFile(image.ImageUrl);

                return Ok(new { message = "تم حذف الصورة بنجاح" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء حذف الصورة", error = ex.Message });
            }
        }
    }
}
