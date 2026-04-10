using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mando.Api.Data;
using Mando.Api.Interfaces.Visits;

namespace Mando.Api.Services.Visits;

public class VisitLifecycleLockService : IVisitLifecycleLockService
{
    private readonly AppDbContext _context;

    public VisitLifecycleLockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> LockAsync(Guid visitId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsSqlServer())
        {
            return await _context.Visits
                .AsNoTracking()
                .AnyAsync(x => x.Id == visitId, cancellationToken);
        }

        var currentTransaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "Visit lifecycle lock requires an active database transaction.");

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = currentTransaction.GetDbTransaction();
        command.CommandText =
            "SELECT TOP (1) 1 FROM [Visits] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @visitId";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@visitId";
        parameter.Value = visitId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }
}
