using System.Data.Common;

namespace AmeCapture.Application.Interfaces;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateConnectionAsync();
    string DatabasePath { get; }
}
