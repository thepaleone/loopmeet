using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly InMemoryStore _store;

    public InMemoryUserRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Users.FirstOrDefault(user => user.Id == id));
        }
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Users.FirstOrDefault(user => user.Email == email));
        }
    }

    public Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Users.Where(user => ids.Contains(user.Id)).ToList();
            return Task.FromResult<IReadOnlyList<User>>(results);
        }
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Users.Add(user);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var index = _store.Users.FindIndex(candidate => candidate.Id == user.Id);
            if (index >= 0)
            {
                _store.Users[index] = user;
            }
        }

        return Task.CompletedTask;
    }
}
