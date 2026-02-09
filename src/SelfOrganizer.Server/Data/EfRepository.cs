using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SelfOrganizer.Core.Interfaces;
using SelfOrganizer.Core.Models;
using SelfOrganizer.Server.Services.Auth;

namespace SelfOrganizer.Server.Data;

/// <summary>
/// Entity Framework implementation of IRepository.
/// Automatically filters by UserId for multi-tenant data isolation.
/// </summary>
public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly SelfOrganizerDbContext _context;
    private readonly IServerAuthService _authService;
    private readonly DbSet<T> _dbSet;

    public EfRepository(SelfOrganizerDbContext context, IServerAuthService authService)
    {
        _context = context;
        _authService = authService;
        _dbSet = context.Set<T>();
    }

    private string GetUserId()
    {
        var userId = _authService.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return userId;
    }

    private IQueryable<T> GetUserFilteredQuery()
    {
        var userId = GetUserId();
        return _dbSet.Where(e => EF.Property<string>(e, "UserId") == userId);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await GetUserFilteredQuery()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await GetUserFilteredQuery()
            .OrderByDescending(e => e.ModifiedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        return await GetUserFilteredQuery()
            .Where(predicate)
            .OrderByDescending(e => e.ModifiedAt)
            .ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var userId = GetUserId();

        // Set timestamps
        entity.CreatedAt = DateTime.UtcNow;
        entity.ModifiedAt = DateTime.UtcNow;

        // Set UserId via shadow property
        var entry = _context.Entry(entity);
        entry.Property("UserId").CurrentValue = userId;

        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var userId = GetUserId();

        // Verify entity belongs to user
        var existing = await GetUserFilteredQuery()
            .FirstOrDefaultAsync(e => e.Id == entity.Id);

        if (existing == null)
        {
            throw new KeyNotFoundException($"Entity with ID {entity.Id} not found.");
        }

        // Detach existing entity to avoid tracking conflicts
        _context.Entry(existing).State = EntityState.Detached;

        // Update entity
        entity.ModifiedAt = DateTime.UtcNow;

        var entry = _context.Entry(entity);
        entry.Property("UserId").CurrentValue = userId;
        entry.State = EntityState.Modified;

        // Don't modify CreatedAt
        entry.Property(e => e.CreatedAt).IsModified = false;

        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetUserFilteredQuery()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = GetUserFilteredQuery();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync();
    }
}
