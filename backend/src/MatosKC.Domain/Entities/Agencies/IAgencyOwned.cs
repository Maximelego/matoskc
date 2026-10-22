namespace MatosKC.Domain.Entities.Agencies;

public interface IAgencyOwned
{
    Guid AgencyId { get; }
    void AssignAgency(Guid agencyId);
}
