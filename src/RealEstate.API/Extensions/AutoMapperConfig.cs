using System.Linq;
using System;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RealEstate.API.Profiles;

namespace RealEstate.API.Extensions
{
    public static class AutoMapperConfig
    {
        public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
        {
            services.AddSingleton(provider => 
            {
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile(new MappingProfile(httpContextAccessor));
                });
                
                return config.CreateMapper();
            });
            
            return services;
        }
    }
} 