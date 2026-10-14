namespace MatosKC.Infrastructure.Persistence.Repositories;

using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using Microsoft.EntityFrameworkCore;

public sealed class InspectionSearchRepository(MatosKCDbContext db) : IInspectionSearchRepository
{
    public async Task<InspectionSearchPage> SearchAsync(InspectionSearchQuery filter, CancellationToken ct = default)
    {
        // Project only list data: no answers, images or PDF blobs, and no query per row.
        var query = from inspection in db.Inspections.AsNoTracking()
                    join rental in db.Rentals on inspection.RentalId equals rental.Id
                    join client in db.Clients on rental.ClientId equals client.Id
                    join equipment in db.Equipments on rental.EquipmentId equals equipment.Id
                    select new { Inspection = inspection, Rental = rental, Client = client, Equipment = equipment };

        if (filter.RentalId.HasValue)
            query = query.Where(item => item.Rental.Id == filter.RentalId);

        if (filter.EquipmentId.HasValue)
            query = query.Where(item => item.Equipment.Id == filter.EquipmentId);

        if (filter.AgencyId.HasValue)
            query = query.Where(item => item.Rental.AgencyId == filter.AgencyId);

        if (filter.CommandNumber is not null)
            query = query.Where(item => item.Client.CommandNumber.Value == filter.CommandNumber);

        if (filter.From.HasValue)
            query = query.Where(item => item.Inspection.StartedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(item => item.Inspection.StartedAt <= filter.To.Value);

        if (filter.Type.HasValue)
            query = query.Where(item => item.Inspection.Type == filter.Type.Value);

        if (filter.Status.HasValue)
            query = query.Where(item => item.Inspection.Status == filter.Status.Value);

        int total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(item => item.Inspection.StartedAt).ThenBy(item => item.Inspection.Id)
            .Skip(filter.Offset).Take(filter.Limit)
            .Select(item => new InspectionListItem(
                item.Inspection.Id, item.Rental.Id, item.Equipment.Id, item.Equipment.Name, item.Equipment.SerialNumber,
                item.Rental.AgencyId, item.Client.CommandNumber.AgencyCode, item.Client.DisplayName,
                item.Client.CommandNumber.Value, item.Inspection.TemplateId, item.Inspection.Type, item.Inspection.Status,
                item.Inspection.OperatorFirstName, item.Inspection.StartedAt, item.Inspection.ValidatedAt,
                item.Inspection.CancelledAt, item.Inspection.ReportPdf != null))
            .ToArrayAsync(ct);

        return new InspectionSearchPage(items, total, filter.Offset, filter.Limit);
    }
}
