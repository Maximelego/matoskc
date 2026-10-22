namespace MatosKC.Application.Agencies;

using MatosKC.Application.Inspections;
using MatosKC.Domain.Entities.Accounts;

// Scoped lifetime. Outside an HTTP request, trusted migrations/seeding use system access.
public sealed class AgencyScope
{
    private InspectionActor? actor;
    private Guid? filter;
    public bool IsSystem { get; private set; } = true;
    public bool IsSuperAdmin => actor?.Role == Role.SuperAdmin;
    public bool CanAccessAll => IsSystem || (IsSuperAdmin && !filter.HasValue);
    public Guid? AgencyId => filter ?? (IsSuperAdmin ? null : actor?.AgencyId);

    public void SetActor(InspectionActor? value, Guid? requestedAgency = null)
    {
        IsSystem = false;
        actor = value;
        filter = null;
        if (value is null) return;
        InspectionWorkflowContext.RequireInspectionReader(value);
        if (requestedAgency == Guid.Empty) throw new InspectionOperationException(400, "Invalid agency identifier.");
        if (!IsSuperAdmin && requestedAgency.HasValue && requestedAgency != value.AgencyId)
            throw new InspectionOperationException(403, "Only your own agency is accessible.");
        if (IsSuperAdmin) filter = requestedAgency;
    }

    public Guid ResolveOwner(Guid? requestedAgency)
    {
        if (!IsSystem && actor is null) throw new InspectionOperationException(403, "An authenticated account is required.");
        var owner = requestedAgency ?? AgencyId;
        if (!owner.HasValue || owner == Guid.Empty)
            throw new InspectionOperationException(400, "Select an agency.");
        EnsureAccess(owner.Value);
        return owner.Value;
    }

    public void EnsureAccess(Guid owner)
    {
        if (owner == Guid.Empty) throw new InspectionOperationException(400, "An agency is required.");
        if (!CanAccessAll && (!AgencyId.HasValue || AgencyId != owner))
            throw new InspectionOperationException(403, "Resource belongs to another agency.");
    }
}
