using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Infrastructure.Data.IDataWrapperFactory
{
    public interface IDbCommand : IDisposable
    {
        string CommandText { get; set; }
        CommandType CommandType { get; set; }
        void AddParameter(string name, object value);
        void AddOutputParameter(string name, SqlDbType type);
        void AddOutputParameter(string name, SqlDbType type, int size);
        object GetParameterValue(string name);
        Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken);
        Task<IDataReader> ExecuteReaderAsync(CancellationToken cancellationToken);
        Task<object> ExecuteScalarAsync(CancellationToken cancellationToken);
    }
}
