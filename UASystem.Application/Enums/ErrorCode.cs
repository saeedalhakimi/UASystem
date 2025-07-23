using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UASystem.Api.Application.Enums
{
    public enum ErrorCode
    {
        OperationCancelled,
        DomainValidationError,
        InternalServerError,
        ResourceAlreadyExists,
        ResourceCreationFailed,
        ResourceNotFound,
        UnknownError,
        InvalidInput,
        ConflictError,
        Unauthorized,
        BadRequest,
        ApplicationValidationError,
        Forbidden,
        NotImplemented,
        ServiceUnavailable,
        GatewayTimeout,
        TooManyRequests,
        PreconditionFailed,
        Locked,
        NoResult,
        InvalidData,
        InvalidOperation,
        DatabaseError,
        AuthorizationError,
        ResourceUpdateFailed,
        ResourceDeletionFailed,
        ValidationError,
        AuthenticationError,
        PermissionDenied,
        RateLimitExceeded,
        TimeoutError,
        NotFound,
        ConcurrencyConflict
    }
}
