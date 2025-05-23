using System.Linq;
using AutoMapper;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;

namespace RealEstate.API.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<ApplicationUser, UserDto>();
            CreateMap<ApplicationUser, UserProfileDto>();

            // Property mappings
            CreateMap<Property, PropertyDto>();
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
                    })));
            CreateMap<PropertyCreateDto, Property>();
            CreateMap<PropertyUpdateDto, Property>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Property image mappings
            CreateMap<PropertyImage, PropertyImageDto>();
            CreateMap<UserUploadedImage, UserUploadedImageDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName));

            // Booking mappings
            CreateMap<Booking, BookingDto>()
                .ForMember(dest => dest.PropertyTitle, opt => opt.MapFrom(src => src.Property.Title))
                .ForMember(dest => dest.PropertyLocation, opt => opt.MapFrom(src => src.Property.Location));
            CreateMap<BookingCreateDto, Booking>();
            CreateMap<Booking, BookingResponseDto>();

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
    }
}
