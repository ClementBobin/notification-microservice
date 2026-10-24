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

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterFcmTokenAsync(Guid userId, string fcmToken, CancellationToken ct)
    {
        var user = await FindByIdAsync(userId, ct);
        if (user is null) throw new InvalidOperationException("User not found");
        user.FcmToken = fcmToken;
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }
}
