namespace MatosKC.Application.Inspections.Ports;

public interface IInspectionSearchRepository
{
    Task<InspectionSearchPage> SearchAsync(InspectionSearchQuery query, CancellationToken ct = default);
}
