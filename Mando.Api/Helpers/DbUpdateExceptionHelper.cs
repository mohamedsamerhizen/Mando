using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Mando.Api.Helpers;

public static class DbUpdateExceptionHelper
{
    private const int SqlServerDuplicateKeyErrorNumber = 2627;
    private const int SqlServerUniqueIndexErrorNumber = 2601;
    private const int SqlServerDeadlockErrorNumber = 1205;
    private const int SqlServerTimeoutErrorNumber = -2;

    public static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return FindSqlException(exception) is { Number: SqlServerDuplicateKeyErrorNumber or SqlServerUniqueIndexErrorNumber }
               || HasInnerException(
                   exception,
                   innerException => innerException.GetType().Name == "SqliteException" &&
                                     innerException.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsTransientSqlFailure(DbUpdateException exception)
    {
        return FindSqlException(exception) is { Number: SqlServerDeadlockErrorNumber or SqlServerTimeoutErrorNumber }
               || HasInnerException(
                   exception,
                   innerException => innerException.GetType().Name == "SqliteException" &&
                                     (innerException.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase)
                                      || innerException.Message.Contains("database is busy", StringComparison.OrdinalIgnoreCase)));
    }

    private static SqlException? FindSqlException(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is SqlException sqlException)
                return sqlException;

            exception = exception.InnerException;
        }

        return null;
    }

    private static bool HasInnerException(Exception? exception, Func<Exception, bool> predicate)
    {
        while (exception is not null)
        {
            if (predicate(exception))
                return true;

            exception = exception.InnerException;
        }

        return false;
    }
}