namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;

public sealed class MvpInspectionTests
{
    [Fact]
    public async Task CancelDeparture_ReleasesOpenRentalAndAllowsNewDepartureAndIsIdempotent()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var useCase = new CancelInspectionUseCase(fake.Context);
        var result = await useCase.ExecuteAsync(inspection.Id, new("Abandon"), fake.Actor);
        Assert.Equal(InspectionStatus.Cancelled, result.Status);
        Assert.Equal(RentalStatus.Cancelled, fake.Rentals[0].Status);
        Assert.Equal(EquipmentStatus.Available, fake.Equipment.Status);
        fake.Clock.Now = fake.Clock.Now.AddDays(1);
        var retry = await useCase.ExecuteAsync(inspection.Id, new("Autre"), fake.Actor);
        Assert.Equal(result.CancelledAt, retry.CancelledAt);
        Assert.Equal("Abandon", retry.CancellationReason);
        var next = await fake.DepartureAsync();
        Assert.NotEqual(inspection.RentalId, next.RentalId);
    }

    [Fact]
    public async Task CancelReturn_KeepsRentalActiveAndAllowsAnotherReturnWithLatestTemplate()
    {
        var fake = new WorkflowFake();
        var departure = await fake.DepartureAsync(validate: true);
        var returns = new CreateReturnInspectionUseCase(fake.Context);
        var first = await returns.ExecuteAsync(departure.RentalId, new("Maxime"), fake.Actor);
        await new CancelInspectionUseCase(fake.Context).ExecuteAsync(first.Id, new("Recommencer"), fake.Actor);
        Assert.Equal(RentalStatus.Active, fake.Rentals[0].Status);
        Assert.Equal(EquipmentStatus.Borrowed, fake.Equipment.Status);
        var latest = fake.AddTemplate();
        var next = await returns.ExecuteAsync(departure.RentalId, new("Maxime"), fake.Actor);
        Assert.NotEqual(first.Id, next.Id);
        Assert.Equal(latest.Id, next.TemplateId);
    }

    [Fact]
    public async Task Cancellation_RejectsValidatedInspectionAdminForeignAgencyAndEmptyReason()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync(validate: true);
        var useCase = new CancelInspectionUseCase(fake.Context);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id, new("Abandon"), fake.Actor));
        Assert.Equal(409, error.StatusCode);
        foreach (var actor in new[] { new InspectionActor(Guid.NewGuid(), Role.Admin, null),
            new InspectionActor(Guid.NewGuid(), Role.Agency, 33, Guid.NewGuid()) })
        {
            error = await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id, new("Abandon"), actor));
            Assert.Equal(403, error.StatusCode);
        }
        error = await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(inspection.Id, new(" "), fake.Actor));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task Search_ScopesAgencyBeforePaginationAndNormalizesDates()
    {
        var fake = new WorkflowFake();
        var port = new SearchPort();
        var useCase = new SearchInspectionsUseCase(port, fake.Context);
        var from = new DateTimeOffset(2026, 10, 14, 14, 0, 0, TimeSpan.FromHours(2));
        await useCase.ExecuteAsync(new(From: from, Status: InspectionStatus.Draft, Offset: 10, Limit: 5), fake.Actor);
        Assert.Equal(fake.Actor.AgencyId, port.Query!.AgencyId);
        Assert.Equal(TimeSpan.Zero, port.Query.From!.Value.Offset);
        Assert.Equal(10, port.Query.Offset);
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() => useCase.ExecuteAsync(
            new(AgencyId: Guid.NewGuid()), fake.Actor));
        Assert.Equal(403, error.StatusCode);
        var admin = new InspectionActor(Guid.NewGuid(), Role.Admin, null);
        await useCase.ExecuteAsync(new(), admin);
        Assert.Null(port.Query.AgencyId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Search_RejectsInvalidFiltersBeforeCallingRepository(int scenario)
    {
        var fake = new WorkflowFake();
        var port = new SearchPort();
        var query = scenario switch
        {
            0 => new InspectionSearchQuery(Offset: -1),
            1 => new InspectionSearchQuery(Limit: 101),
            2 => new InspectionSearchQuery(CommandNumber: "123"),
            3 => new InspectionSearchQuery(Status: (InspectionStatus)99),
            _ => new InspectionSearchQuery(From: fake.Clock.Now, To: fake.Clock.Now.AddDays(-1))
        };
        var error = await Assert.ThrowsAsync<InspectionOperationException>(() =>
            new SearchInspectionsUseCase(port, fake.Context).ExecuteAsync(query, fake.Actor));
        Assert.Equal(400, error.StatusCode);
        Assert.Null(port.Query);
    }

    private sealed class SearchPort : IInspectionSearchRepository
    {
        public InspectionSearchQuery? Query { get; private set; }

        public Task<InspectionSearchPage> SearchAsync(InspectionSearchQuery query, CancellationToken ct = default)
        {
            Query = query;

            return Task.FromResult(new InspectionSearchPage([], 0, query.Offset, query.Limit));
        }
    }
}
