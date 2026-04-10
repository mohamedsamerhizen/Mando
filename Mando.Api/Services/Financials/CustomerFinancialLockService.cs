using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mando.Api.Data;
using Mando.Api.Interfaces.Financials;

namespace Mando.Api.Services.Financials;

public class CustomerFinancialLockService : ICustomerFinancialLockService
{
    private readonly AppDbContext _context;

    public CustomerFinancialLockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> LockAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsSqlServer())
        {
            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(x => x.Id == customerId, cancellationToken);
        }

        var currentTransaction = _context.Database.CurrentTransaction
            ?? throw new InvalidOperationException(
                "Customer financial lock requires an active database transaction.");

        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = currentTransaction.GetDbTransaction();
        command.CommandText =
            "SELECT TOP (1) 1 FROM [Customers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = @customerId";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@customerId";
        parameter.Value = customerId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull;
    }
}