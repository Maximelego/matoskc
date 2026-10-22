namespace MatosKC.Application.Inspections;

using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;

public sealed record InspectionSearchQuery(
    Guid? RentalId = null,
    Guid? EquipmentId = null,
    Guid? AgencyId = null,
    string? CommandNumber = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    InspectionType? Type = null,
    InspectionStatus? Status = null,
    int Offset = 0,
    int Limit = 50
);

public sealed record InspectionListItem(
    Guid Id,
    Guid RentalId,
    Guid EquipmentId,
    string EquipmentName,
    string SerialNumber,
    Guid? AgencyId,
    int AgencyCode,
    string ClientName,
    string CommandNumber,
    Guid TemplateId,
    InspectionType Type,
    InspectionStatus Status,
    string OperatorFirstName,
    DateTimeOffset StartedAt,
    DateTimeOffset? ValidatedAt,
    DateTimeOffset? CancelledAt,
    bool HasReport
);

public sealed record InspectionSearchPage(
    IReadOnlyList<InspectionListItem> Inspections,
    int Total,
    int Offset,
    int Limit
);

public sealed class SearchInspectionsUseCase(
    IInspectionSearchRepository repository,
    InspectionWorkflowContext context
)
{
    public async Task<InspectionSearchPage> ExecuteAsync(
        InspectionSearchQuery query,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireInspectionReader(actor);

        if (query.Offset < 0 || query.Limit is < 1 or > 100
            || (query.Type.HasValue && !Enum.IsDefined(query.Type.Value))
            || (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
            || (query.From.HasValue && query.To.HasValue && query.From > query.To)
            || query.RentalId == Guid.Empty || query.EquipmentId == Guid.Empty || query.AgencyId == Guid.Empty
            || (query.CommandNumber is not null && (query.CommandNumber.Length != 6
                || query.CommandNumber.Any(character => character is < '0' or > '9'))))
        {
            throw new InspectionOperationException(400, "Invalid inspection search filters or pagination.");
        }

        if (actor.Role is Role.Agency or Role.Admin)
        {
            if (!actor.AgencyId.HasValue || actor.AgencyId == Guid.Empty
                || (query.AgencyId.HasValue && query.AgencyId != actor.AgencyId))
            {
                throw new InspectionOperationException(403, "This account can only search its own agency.");
            }

            query = query with { AgencyId = actor.AgencyId };
        }

        if (query.RentalId.HasValue)
        {
            await context.GetRentalAsync(query.RentalId.Value, actor, ct);
        }

        query = query with
        {
            From = query.From?.ToUniversalTime(),
            To = query.To?.ToUniversalTime()
        };

        return await repository.SearchAsync(query, ct);
    }
}
