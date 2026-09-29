using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Users;

namespace SapDiApi.Bridge.Services.Users
{
    public interface IUserService
    {
        IQueryable<UserDto> GetUsersQueryable();
        Task<UserDto?> GetByIdAsync(int internalKey, bool includePermissions = false, UserSession? session = null);
        Task<UserDto?> GetByCodeAsync(string userCode, bool includePermissions = false, UserSession? session = null);
        Task<IEnumerable<UserDto>> GetFilteredAsync(UserFilterDto filter, UserSession? session = null);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> CreateAsync(CreateUserDto dto, UserSession? session = null);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateAsync(int internalKey, UpdateUserDto dto, UserSession? session = null);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateByCodeAsync(string userCode, UpdateUserDto dto, UserSession? session = null);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangePasswordAsync(int internalKey, ChangeUserPasswordDto dto, UserSession? session = null);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangePasswordByCodeAsync(string userCode, ChangeUserPasswordDto dto, UserSession? session = null);
    }
}
