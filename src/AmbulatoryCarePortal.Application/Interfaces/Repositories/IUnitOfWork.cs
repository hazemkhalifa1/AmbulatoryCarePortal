using AmbulatoryCarePortal.Application.Interfaces.Repositories;
using AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    public Task<int> SaveChangesAsync();
    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
}
