using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Interfaces.Contracts
{
    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Role { get; set; } = "Cliente";

        public decimal? InitialAmount { get; set; }
    }

    public class UpdateUserRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }

        public decimal? AdditionalAmount { get; set; }
    }

    public class UserOperationResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }
        public string? UserId { get; set; }

        public string? Warning { get; set; }

        public static UserOperationResult Success(string? userId, string? warning = null)
            => new UserOperationResult { Succeeded = true, UserId = userId, Warning = warning };

        public static UserOperationResult Failure(string error)
            => new UserOperationResult { Succeeded = false, Error = error };
    }

    public interface IUserManagementService
    {
        Task<PagedResult<UserInfoDto>> GetPagedAsync(int page, int pageSize, string? role = null,
            CancellationToken cancellationToken = default);

        Task<UserOperationResult> CreateAsync(CreateUserRequest request, string? createdByUserId,
            bool sendTokenInsteadOfLink = false, CancellationToken cancellationToken = default);

        Task<UserOperationResult> CreateCommerceUserAsync(CreateUserRequest request, int commerceId,
            bool sendTokenInsteadOfLink = false, CancellationToken cancellationToken = default);

        Task<UserOperationResult> UpdateAsync(UpdateUserRequest request, string? updatedByUserId,
            CancellationToken cancellationToken = default);

        Task<UserOperationResult> SetActiveAsync(string targetUserId, bool isActive, string? callerUserId,
            CancellationToken cancellationToken = default);

        Task<int> DeactivateUsersOfCommerceAsync(IEnumerable<string> userIds,
            CancellationToken cancellationToken = default);

        Task<bool> UserNameExistsAsync(string userName, string? excludeUserId = null);
        Task<bool> EmailExistsAsync(string email, string? excludeUserId = null);
        Task<bool> IdentificationExistsAsync(string identification, string? excludeUserId = null);
    }
}
