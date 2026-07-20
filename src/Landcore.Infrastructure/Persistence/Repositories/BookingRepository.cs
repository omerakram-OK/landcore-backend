using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly MongoDbContext _context;

    public BookingRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Booking>.Filter.Eq(x => x.Id, id)
                     & Builders<Booking>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Booking>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Bookings.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Booking>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Booking>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Bookings.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Booking?> GetActiveByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Booking>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Booking>.Filter.Eq(x => x.PlotId, plotId)
                     & Builders<Booking>.Filter.Eq(x => x.Status, BookingStatus.Active)
                     & Builders<Booking>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Bookings.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Booking?> GetMostRecentByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Booking>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Booking>.Filter.Eq(x => x.PlotId, plotId)
                     & Builders<Booking>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Bookings.Find(filter).SortByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.InsertOneAsync(booking, options: null, cancellationToken);
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.ReplaceOneAsync(x => x.Id == booking.Id, booking, cancellationToken: cancellationToken);
    }
}
