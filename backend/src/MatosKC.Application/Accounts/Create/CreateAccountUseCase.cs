using MatosKC.Application.Agencies.Ports;
namespace MatosKC.Application.Accounts.Create;

using MatosKC.Application.Accounts.Ports;
using MatosKC.Domain.Entities.Accounts;

public class CreateAccountUseCase
{

    private readonly IAccountRepository _accountRepository;
    private readonly IAgencyRepository _agencyRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateAccountUseCase(IAccountRepository accountRepository, IAgencyRepository agencyRepository, IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _agencyRepository = agencyRepository;
        _passwordHasher = passwordHasher;
    }


    public async Task<Guid> ExecuteAsync(CreateAccountDto dto, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new InvalidOperationException("Password cannot be null or empty.");

        var hashedPassword = _passwordHasher.HashPassword(dto.Password);

        if (dto.Email != null && dto.Role != Role.Agency)
        {
            if (await _accountRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
                throw new InvalidOperationException($"An account with the specified email already exists ({dto.Email}).");
        }

        if (dto.Role != Role.SuperAdmin && dto.AgencyId != null)
        {
            Guid agencyId = dto.AgencyId.Value;
            if (!await _agencyRepository.ExistsByIdAsync(agencyId, cancellationToken))
                throw new InvalidOperationException($"The specified agency does not exist ({dto.AgencyId}).");

            if (dto.Role == Role.Agency && await _accountRepository.ExistsAgencyAccountAsync(agencyId, cancellationToken))
                throw new InvalidOperationException($"An account for the specified agency already exists ({dto.AgencyId}).");
        }

        var account = new Account(
            dto.DisplayName,
            dto.Email,
            hashedPassword,
            true,
            dto.Role,
            dto.AgencyId
        );

        var result = await _accountRepository.AddAsync(account, cancellationToken);
        return result.Id;
    }
}
