using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Infrastructure.Data.IDataWrapperFactory
{
    public interface IDatabaseConnectionFactory
    {
        Task<IDatabaseConnection> CreateConnectionAsync(string connectionString, CancellationToken cancellationToken);
    }
}
