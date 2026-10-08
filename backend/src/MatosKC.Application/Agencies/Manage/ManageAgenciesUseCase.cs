using MatosKC.Application.Administration;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Domain.Entities.Agencies;
namespace MatosKC.Application.Agencies.Manage;

public sealed record AgencyDto(string Name, int Code);
public sealed class ManageAgenciesUseCase(IAgencyRepository agencies, IAdministrationRepository administration)
{
    public async Task<Agency> CreateAsync(AgencyDto dto, CancellationToken ct = default)
    {
        var agency = new Agency(dto.Name, dto.Code);
        await EnsureUniqueAsync(dto.Code, null, ct);
        return await agencies.AddAsync(agency, ct);
    }
    public async Task<Agency> UpdateAsync(Guid id, AgencyDto dto, CancellationToken ct = default)
    {
        var agency = await GetAsync(id, ct);
        await EnsureUniqueAsync(dto.Code, id, ct);
        agency.Update(dto.Name, dto.Code);
        return await administration.UpdateAgencyAsync(agency, ct);
    }
    public async Task<Agency> GetAsync(Guid id, CancellationToken ct = default) =>
        await agencies.GetByIdAsync(id, ct) ?? throw new AdministrationException(404, "Agency not found.");
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var agency = await GetAsync(id, ct);
        if (await administration.AgencyHasAccountsAsync(id, ct))
            throw new AdministrationException(409, "Delete the agency accounts first.");
        await administration.DeleteAgencyAsync(agency, ct);
    }
    private async Task EnsureUniqueAsync(int code, Guid? exceptId, CancellationToken ct)
    {
        if (await administration.AgencyCodeExistsAsync(code, exceptId, ct))
            throw new AdministrationException(409, "Agency code already exists.");
    }
}
