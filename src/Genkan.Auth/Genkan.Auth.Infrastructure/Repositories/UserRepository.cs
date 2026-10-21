using Genkan.Shared.Entities;
using Genkan.Auth.Infrastructure.Data;
using Genkan.Auth.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Auth.Infrastructure.Repositories;

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
