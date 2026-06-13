using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;

namespace AmbulatoryCarePortal.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id, bool includeDeleted = false)
    {
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<T>> GetAllAsync(bool includeDeleted = false)
    {
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false)
    {
        var query = _dbSet.Where(predicate);
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false)
    {
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false)
    {
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.AnyAsync(predicate);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, bool includeDeleted = false)
    {
        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        Update(entity);
    }

    public void SoftDeleteRange(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
            entity.IsDeleted = true;
        UpdateRange(entities);
    }

    private const int MaxPageSize = 100;

    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool ascending = true, bool includeDeleted = false)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();

        if (orderBy != null)
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<T>> GetPagedWithIncludesAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool ascending = true, bool includeDeleted = false, params Expression<Func<T, object>>[] includes)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _dbSet.AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();

        if (orderBy != null)
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<T>> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, bool includeDeleted = false, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.Where(predicate);
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (includes != null)
        {
            foreach (var include in includes)
                query = query.Include(include);
        }

        return await query.ToListAsync();
    }
}
