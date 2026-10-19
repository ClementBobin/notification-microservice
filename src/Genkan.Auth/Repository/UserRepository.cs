using Genkan.Auth.Data;
using Genkan.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Auth.Repository;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<bool> AnyAsync(CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}

public sealed class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct) => db.Users.AnyAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
