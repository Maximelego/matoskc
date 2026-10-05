using MatosKC.Application.Agencies.Ports;
namespace MatosKC.Application.Accounts.Create;

using MatosKC.Application.Accounts.Ports;
using MatosKC.Domain.Entities.Accounts;

public class CreateAccountUseCase
{

    private readonly IAccountRepository _accountRepository;
    private readonly IAgencyRepository _agencyRepository;

    public CreateAccountUseCase(IAccountRepository accountRepository, IAgencyRepository agencyRepository)
    {
        _accountRepository = accountRepository;
        _agencyRepository = agencyRepository;
    }


    public async Task ExecuteAsync(CreateAccountDto dto, CancellationToken cancellationToken = default)
    {
        if (await _accountRepository.ExistsByEmailAsync(dto.Email ?? string.Empty, cancellationToken))
        {
            throw new InvalidOperationException("An account with the provided email already exists.");
        }

        if (dto.Role != Role.SuperAdmin && dto.AgencyId != null)
        {
            Guid agencyId = dto.AgencyId.Value;
            if (!await _agencyRepository.ExistsByIdAsync(agencyId, cancellationToken))
                throw new InvalidOperationException($"The specified agency does not exist ({dto.AgencyId}).");
        }

        var account = new Account(
            dto.DisplayName,
            dto.Email,
            dto.Password,
            true,
            dto.Role,
            dto.AgencyId
        );

        await _accountRepository.AddAsync(account, cancellationToken);
    }
}
