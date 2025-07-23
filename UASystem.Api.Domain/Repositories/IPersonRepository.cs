using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UASystem.Api.Domain.Aggregate;

namespace UASystem.Api.Domain.Repositories
{
    public interface IPersonRepository
    {
        Task<bool> CreateAsync(Person person, Guid createdBy, string? correlationId, CancellationToken cancellationToken);
        Task<Person?> GetByIdAsync(Guid personId, bool includeDeleted, string? correlationId, CancellationToken cancellationToken);
        Task<(bool, byte[]?)> UpdateAsync(Person person, string? correlationId, CancellationToken cancellationToken);
    }
}
