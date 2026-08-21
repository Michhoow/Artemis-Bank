namespace ArtemisBank.Core.Application.Dtos.Commerces
{
    public class CommerceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool HasAssociatedUser { get; set; }

        public DateTime CreatedAt { get; set; }

        public string AccountNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    public class CommerceDetailDto : CommerceDto
    {
        public AssociatedUserDto? AssociatedUser { get; set; }
    }

    public class AssociatedUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class SaveCommerceDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
    }

    public class CommerceStatusDto
    {
        public bool IsActive { get; set; }
    }
}
