using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;
using RealEstate.Infrastructure.Data;

namespace RealEstate.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private bool _disposed = false;
        private readonly ConcurrentDictionary<string, object> _repositories = new();

        private IRepository<Property>? _propertyRepository;
        private IRepository<PropertyImage>? _propertyImageRepository;
        private IRepository<Booking>? _bookingRepository;
        private IRepository<Favorite>? _favoriteRepository;
        private IRepository<RefreshToken>? _refreshTokenRepository;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<Property> Properties =>
            _propertyRepository ??= new Repository<Property>(_context);

        public IRepository<PropertyImage> PropertyImages =>
            _propertyImageRepository ??= new Repository<PropertyImage>(_context);

        public IRepository<Booking> Bookings =>
            _bookingRepository ??= new Repository<Booking>(_context);

        public IRepository<Favorite> Favorites =>
            _favoriteRepository ??= new Repository<Favorite>(_context);

        public IRepository<RefreshToken> RefreshTokens =>
            _refreshTokenRepository ??= new Repository<RefreshToken>(_context);

        public IRepository<T> Repository<T>() where T : class
        {
            var typeName = typeof(T).Name;
            return (IRepository<T>)_repositories.GetOrAdd(typeName, _ => new Repository<T>(_context));
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
