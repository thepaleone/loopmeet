using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly Client _client;

    public UserRepository(Client client)
    {
        _client = client;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdInternalAsync(id);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return GetByEmailInternalAsync(email);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = Map(user);
        await _client.From<UserRecord>().Insert(record);
    }

    public async Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<User>();
        }

        var response = await _client.From<UserRecord>().Get();
        return response.Models
            .Where(user => ids.Contains(user.Id))
            .Select(Map)
            .ToList();
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var record = Map(user);
        await _client.From<UserRecord>().Update(record);
    }

    private async Task<User?> GetByIdInternalAsync(Guid id)
    {
        var response = await _client
            .From<UserRecord>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private async Task<User?> GetByEmailInternalAsync(string email)
    {
        var response = await _client
            .From<UserRecord>()
            .Filter("email", Operator.Equals, email)
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    private static User Map(UserRecord record)
    {
        return new User
        {
            Id = record.Id,
            DisplayName = record.DisplayName,
            Email = record.Email,
            Phone = record.Phone,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static UserRecord Map(User user)
    {
        return new UserRecord
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
