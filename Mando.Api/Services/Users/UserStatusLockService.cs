using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mando.Api.Data;
using Mando.Api.Interfaces.Users;

namespace Mando.Api.Services.Users;

public class UserStatusLockService : IUserStatusLockService
{
    private readonly AppDbContext _context;

    public UserStatusLockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> LockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsSqlServer())
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id == userId, cancellationToken);
        }

        var currentTransaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "User status lock requires an active database transaction.");

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = currentTransaction.GetDbTransaction();
        command.CommandText =
            "SELECT TOP (1) 1 FROM [AspNetUsers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @userId";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@userId";
        parameter.Value = userId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }
}
