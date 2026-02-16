using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly LoopMeetDbContext _dbContext;

    public UserRepository(LoopMeetDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
