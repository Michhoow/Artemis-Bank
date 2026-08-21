using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBank.Core.Application.Services
{
    public class CommerceService : GenericService<Commerce, CommerceDto>, ICommerceService
    {
        private readonly ICommerceRepository _commerceRepository;
        private readonly IUserManagementService _userManagementService;
        private readonly IUserReadService _userReadService;
        private readonly ILogger<CommerceService> _logger;

        public CommerceService(
            ICommerceRepository commerceRepository,
            IUserManagementService userManagementService,
            IUserReadService userReadService,
            IGenericRepository<Commerce> genericRepository,
            IMapper mapper,
            ILogger<CommerceService> logger)
            : base(genericRepository, mapper)
        {
            _commerceRepository = commerceRepository;
            _userManagementService = userManagementService;
            _userReadService = userReadService;
            _logger = logger;
        }

        public override Task<CommerceDto?> AddAsync(CommerceDto dto)
            => throw new BusinessRuleException("Use CreateAsync, que valida la unicidad de RNC y correo.");

        public override Task<bool> DeleteAsync(int id)
            => throw new BusinessRuleException(
                "Los comercios no se eliminan: se desactivan mediante SetStatusAsync.");

        public async Task<PagedResult<CommerceDto>> GetPagedAsync(int page, int pageSize,
            string? status = null, CancellationToken cancellationToken = default)
        {
            if (page <= 0) page = 1;
            if (pageSize is <= 0 or > PagedResult<CommerceDto>.MaxPageSize)
                pageSize = PagedResult<CommerceDto>.MaxPageSize;

            var baseQuery = _commerceRepository.GetAllQuery().AsNoTracking();

            var normalized = (status ?? "activo").Trim().ToLowerInvariant();
            baseQuery = normalized switch
            {
                "inactivo" => baseQuery.Where(c => !c.IsActive),
                "todos" => baseQuery,
                _ => baseQuery.Where(c => c.IsActive)
            };

            var query = baseQuery
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<CommerceDto>.Create(items.Select(Map).ToList(), page, pageSize, total);
        }

        public async Task<CommerceDetailDto?> GetDetailAsync(int commerceId,
            CancellationToken cancellationToken = default)
        {
            var commerce = await _commerceRepository.GetByIdAsync(commerceId);
            if (commerce == null) return null;

            var detail = new CommerceDetailDto
            {
                Id = commerce.Id,
                Name = commerce.Name,
                Description = commerce.Description,
                Email = commerce.Email,
                PhoneNumber = commerce.Phone,
                Rnc = commerce.Rnc,
                IsActive = commerce.IsActive,
                HasAssociatedUser = !string.IsNullOrWhiteSpace(commerce.UserId),
                CreatedAt = commerce.CreatedAt,
                AccountNumber = commerce.AccountNumber,
                UserId = commerce.UserId
            };

            if (!string.IsNullOrWhiteSpace(commerce.UserId))
            {
                var user = await _userReadService.GetByIdAsync(commerce.UserId);
                if (user != null)
                    detail.AssociatedUser = new AssociatedUserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        IsActive = user.IsActive
                    };
            }

            return detail;
        }

        public async Task<CommerceDto?> GetByUserIdAsync(string userId,
            CancellationToken cancellationToken = default)
        {
            var commerce = await _commerceRepository.GetByUserIdAsync(userId, cancellationToken);
            return commerce == null ? null : Map(commerce);
        }

        public async Task<CommerceDto> CreateAsync(SaveCommerceDto request, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            var rnc = (request.Rnc ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();

            if (await _commerceRepository.RncExistsAsync(rnc, cancellationToken))
                throw new ConflictException(AppMessages.CommerceRncAlreadyExists);

            if (await _commerceRepository.EmailExistsAsync(email, cancellationToken))
                throw new ConflictException(AppMessages.CommerceEmailAlreadyExists);

            var commerce = new Commerce
            {
                Name = (request.Name ?? string.Empty).Trim(),
                Rnc = rnc,
                Email = email,
                Description = string.IsNullOrWhiteSpace(request.Description)
                    ? null : request.Description!.Trim(),
                Phone = (request.PhoneNumber ?? string.Empty).Trim(),

                IsActive = true,
                UserId = string.Empty,
                AccountNumber = string.Empty,
                CreatedAt = DateTime.Now,
                CreatedByUserId = adminUserId
            };

            await _commerceRepository.AddAsync(commerce);

            _logger.LogInformation("Comercio '{Name}' (RNC {Rnc}) creado por {AdminId}.",
                commerce.Name, commerce.Rnc, adminUserId);

            return Map(commerce);
        }

        public async Task<CommerceDto> UpdateAsync(int commerceId, SaveCommerceDto request,
            CancellationToken cancellationToken = default)
        {
            var commerce = await _commerceRepository.GetByIdAsync(commerceId)
                           ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            var rnc = (request.Rnc ?? string.Empty).Trim();
            var email = (request.Email ?? string.Empty).Trim();

            if (!string.Equals(commerce.Rnc, rnc, StringComparison.OrdinalIgnoreCase) &&
                await _commerceRepository.RncExistsAsync(rnc, cancellationToken))
                throw new ConflictException(AppMessages.CommerceRncAlreadyExists);

            if (!string.Equals(commerce.Email, email, StringComparison.OrdinalIgnoreCase) &&
                await _commerceRepository.EmailExistsAsync(email, cancellationToken))
                throw new ConflictException(AppMessages.CommerceEmailAlreadyExists);

            commerce.Name = (request.Name ?? string.Empty).Trim();
            commerce.Rnc = rnc;
            commerce.Email = email;
            commerce.Phone = (request.PhoneNumber ?? string.Empty).Trim();
            commerce.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null : request.Description!.Trim();

            await _commerceRepository.UpdateEntityAsync(commerce);

            _logger.LogInformation("Comercio {CommerceId} actualizado.", commerceId);

            return Map(commerce);
        }

        public async Task<CommerceDto> SetStatusAsync(int commerceId, bool isActive, string? adminUserId,
            CancellationToken cancellationToken = default)
        {
            var commerce = await _commerceRepository.GetByIdAsync(commerceId)
                           ?? throw new NotFoundException(AppMessages.CommerceNotFound);

            commerce.IsActive = isActive;
            await _commerceRepository.UpdateEntityAsync(commerce);

            if (!isActive && !string.IsNullOrWhiteSpace(commerce.UserId))
            {
                var affected = await _userManagementService.DeactivateUsersOfCommerceAsync(
                    new[] { commerce.UserId }, cancellationToken);

                _logger.LogInformation(
                    "Comercio {CommerceId} desactivado. Usuarios asociados inactivados: {Count}.",
                    commerceId, affected);
            }
            else
            {
                _logger.LogInformation(
                    "Comercio {CommerceId} activado por {AdminId}. Sus usuarios NO fueron reactivados automáticamente.",
                    commerceId, adminUserId);
            }

            return Map(commerce);
        }

        private static CommerceDto Map(Commerce commerce) => new CommerceDto
        {
            Id = commerce.Id,
            Name = commerce.Name,
            Description = commerce.Description,
            Rnc = commerce.Rnc,
            Email = commerce.Email,
            PhoneNumber = commerce.Phone,
            AccountNumber = commerce.AccountNumber,
            UserId = commerce.UserId,
            IsActive = commerce.IsActive,
            HasAssociatedUser = !string.IsNullOrWhiteSpace(commerce.UserId),
            CreatedAt = commerce.CreatedAt
        };
    }
}
