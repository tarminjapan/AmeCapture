using System.Data.Common;

namespace AmeCapture.Application.Interfaces
{
    public interface IDbConnectionFactory
    {
        public Task<DbConnection> CreateConnectionAsync();
        public string DatabasePath { get; }
    }
}
