using System.Linq;
using System;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;

namespace RealEstate.API.Profiles
{
    public class MappingProfile : Profile
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        
        public MappingProfile(IHttpContextAccessor? httpContextAccessor = null)
        {
            _httpContextAccessor = httpContextAccessor;
            
            // Get the base URL for the application
            string baseUrl = GetBaseUrl();

            // User mappings
            CreateMap<ApplicationUser, UserDto>();
            CreateMap<ApplicationUser, UserProfileDto>();

            // Property mappings
            CreateMap<Property, PropertyDto>()
                .ForMember(dest => dest.IsForRent, opt => opt.MapFrom(src => src.RentalDurationMonths != null || src.RentalEndDate != null))
                .ForMember(dest => dest.IsForSale, opt => opt.MapFrom(src => src.RentalDurationMonths == null && src.RentalEndDate == null))
                .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.MainImageUrl));
            
            CreateMap<Property, PropertyDetailsDto>()
                .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => new PropertyOwnerDto
                {
                    Id = src.Owner.Id,
                    Name = src.Owner.FullName,
                    PhoneNumber = src.Owner.PhoneNumber ?? string.Empty,
                    Email = src.Owner.Email ?? string.Empty
                }))
                .ForMember(dest => dest.UserUploadedImages, opt => opt.MapFrom(src => 
                    src.UserUploadedImages.Where(i => i.IsApproved).Select(i => new UserUploadedImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        FileName = i.FileName,
                        Description = i.Description,
                        UploadedAt = i.UploadedAt,
                        IsApproved = i.IsApproved,
                        UserName = i.User.FullName
                    })))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => 
                    src.Images.Select(i => new PropertyImageDto
                    {
                        Id = i.Id,
                        Url = i.Url,
                        Description = i.Description,
                        Order = i.Order
                    })))
                .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src => src.MainImageUrl));
            
            CreateMap<PropertyCreateDto, Property>()
                .ForMember(dest => dest.IsForRent, opt => opt.Ignore())
                .ForMember(dest => dest.IsForSale, opt => opt.Ignore());
            CreateMap<PropertyUpdateDto, Property>()
                .ForMember(dest => dest.IsForRent, opt => opt.Ignore())
                .ForMember(dest => dest.IsForSale, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Property image mappings
            CreateMap<PropertyImage, PropertyImageDto>();
            
            CreateMap<UserUploadedImage, UserUploadedImageDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

            // Booking mappings
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => new PropertyBasicInfoDto
                {
                    Title = src.Property.Title,
                    Location = src.Property.Location,
                    MainImageUrl = src.Property.MainImageUrl,
                    Price = src.Property.Price
                }));
            CreateMap<BookingCreateDto, Booking>();
            CreateMap<Booking, BookingResponseDto>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => new PropertyBasicInfoDto
                {
                    Title = src.Property.Title,
                    Location = src.Property.Location,
                    MainImageUrl = src.Property.MainImageUrl,
                    Price = src.Property.Price
                }));

            // Favorite mappings
            CreateMap<Favorite, FavoriteDto>()
                .ForMember(dest => dest.Property, opt => opt.MapFrom(src => new FavoritePropertyDto
                {
                    Title = src.Property.Title,
                    Price = src.Property.Price,
                    Location = src.Property.Location,
                    Bedrooms = src.Property.Bedrooms,
                    Area = src.Property.Area,
                    MainImageUrl = src.Property.MainImageUrl,
                    IsAvailable = src.Property.IsAvailable
                }));
        }

        private string GetBaseUrl()
        {
            // Try to get the base URL from the current request
            if (_httpContextAccessor != null && _httpContextAccessor.HttpContext != null)
            {
                var request = _httpContextAccessor.HttpContext.Request;
                return $"{request.Scheme}://{request.Host}";
            }
            
            // Fallback to environment variable or default
            return Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5268";
        }

        private string FormatImageUrl(string imageUrl, string baseUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return string.Empty;

            // If already a full URL, return as is
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                return imageUrl;

            // Remove leading slash if present
            if (imageUrl.StartsWith("/"))
                imageUrl = imageUrl[1..];

            // Combine with base URL
            return $"{baseUrl.TrimEnd('/')}/{imageUrl}";
        }
    }
}
