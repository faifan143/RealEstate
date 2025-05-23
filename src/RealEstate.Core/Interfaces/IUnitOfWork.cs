using System;
using System.Threading.Tasks;
using RealEstate.Core.Entities;

namespace RealEstate.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Property> Properties { get; }
        IRepository<PropertyImage> PropertyImages { get; }
        IRepository<Booking> Bookings { get; }
        IRepository<Favorite> Favorites { get; }
        IRepository<RefreshToken> RefreshTokens { get; }

        // Generic repository access
        IRepository<T> Repository<T>() where T : class;

        Task<int> CompleteAsync();
    }
}
