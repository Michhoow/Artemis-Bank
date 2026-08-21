namespace ArtemisBank.Core.Application.Interfaces
{
    public interface IAuthenticatedUser
    {
        string? UserId { get; }
        string? UserName { get; }
        string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
