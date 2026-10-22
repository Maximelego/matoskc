namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class AdminInspectionAccessTests
{
    private static InspectionActor Admin(WorkflowFake fake) => new(Guid.NewGuid(), Role.Admin, 83, fake.Agency.Id);
    private static InspectionActor SuperAdmin => new(Guid.NewGuid(), Role.SuperAdmin, null);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Search_AlwaysAppliesAccountAgencyBeforePagination(bool explicitAgency)
    {
        var fake = new WorkflowFake();
        var port = new SearchPort();
        var actor = Admin(fake);
        await new SearchInspectionsUseCase(port, fake.Context).ExecuteAsync(
            new(AgencyId: explicitAgency ? actor.AgencyId : null, EquipmentId: fake.Equipment.Id,
                Offset: 10, Limit: 5, CommandNumber: "001234"), actor);
        Assert.Equal(actor.AgencyId, port.Query!.AgencyId);
        Assert.Equal(fake.Equipment.Id, port.Query.EquipmentId);
        Assert.Equal(10, port.Query.Offset);
        Assert.Equal(5, port.Query.Limit);
        Assert.Equal("001234", port.Query.CommandNumber);
    }

    [Fact]
    public async Task Search_RejectsForeignAgencyAndForeignRentalBeforeCallingRepository()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var port = new SearchPort();
        var search = new SearchInspectionsUseCase(port, fake.Context);
        await Forbidden(() => search.ExecuteAsync(new(AgencyId: Guid.NewGuid()), Admin(fake)));
        // Same human-readable agency code is insufficient: authorization uses the agency UUID.
        var foreignAdmin = new InspectionActor(Guid.NewGuid(), Role.Admin, 83, Guid.NewGuid());
        await Forbidden(() => search.ExecuteAsync(new(RentalId: inspection.RentalId), foreignAdmin));
        Assert.Null(port.Query);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrEmptyAgencyCannotFallBackToGlobalAccess(bool empty)
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var actor = new InspectionActor(Guid.NewGuid(), Role.Admin, 83, empty ? Guid.Empty : null);
        var port = new SearchPort();
        await Forbidden(() => new SearchInspectionsUseCase(port, fake.Context).ExecuteAsync(new(), actor));
        var query = new QueryInspectionsUseCase(fake.Context);
        await Forbidden(() => query.GetAsync(inspection.Id, actor));
        await Forbidden(() => query.GetRentalAsync(inspection.RentalId, actor));
        await Forbidden(() => query.GetOpenRentalAsync(fake.Equipment.Id, actor));
        await Forbidden(() => query.ListAsync(actor, null, null, null));
        Assert.Null(port.Query);
    }

    [Fact]
    public async Task ForeignAdminCannotReadAnyRentalInspectionOrPrivateMediaRoute()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync(true);
        var actor = new InspectionActor(Guid.NewGuid(), Role.Admin, 83, Guid.NewGuid());
        var query = new QueryInspectionsUseCase(fake.Context);
        await Forbidden(() => query.GetAsync(inspection.Id, actor));
        await Forbidden(() => query.GetRentalAsync(inspection.RentalId, actor));
        await Forbidden(() => query.GetDepartureAsync(inspection.RentalId, actor));
        await Forbidden(() => query.GetOpenRentalAsync(fake.Equipment.Id, actor));
        await Forbidden(() => query.ListAsync(actor, inspection.RentalId, null, null));
        await Forbidden(() => new DownloadInspectionReportUseCase(fake.Context).ExecuteAsync(inspection.Id, actor));
        await Forbidden(() => new DownloadInspectionSignatureUseCase(fake.Context).ExecuteAsync(inspection.Id, actor));
        await Forbidden(() => new ManageInspectionPhotosUseCase(fake.Context).DownloadAsync(inspection.Id, Guid.NewGuid(), actor));
    }

    [Fact]
    public async Task OwnAgencyAdminCanReadRentalInspectionReportSignatureAndPhotos()
    {
        var fake = new WorkflowFake();
        var inspection = await fake.DepartureAsync();
        var photoUseCase = new ManageInspectionPhotosUseCase(fake.Context);
        var photo = await photoUseCase.UploadAsync(inspection.Id, inspection.Answers[0].Id,
            new MemoryStream([1]), "photo.png", "image/png", 1, fake.Actor);
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, fake.Actor);
        var admin = Admin(fake);
        var query = new QueryInspectionsUseCase(fake.Context);
        Assert.Equal(inspection.Id, (await query.GetAsync(inspection.Id, admin)).Id);
        Assert.Equal(inspection.RentalId, (await query.GetRentalAsync(inspection.RentalId, admin)).Id);
        Assert.Equal(inspection.Id, (await query.GetDepartureAsync(inspection.RentalId, admin))!.Id);
        Assert.Equal(inspection.RentalId, (await query.GetOpenRentalAsync(fake.Equipment.Id, admin))!.Id);
        Assert.NotEmpty(await new DownloadInspectionReportUseCase(fake.Context).ExecuteAsync(inspection.Id, admin));
        Assert.Equal(new byte[] { 1 }, await new DownloadInspectionSignatureUseCase(fake.Context).ExecuteAsync(inspection.Id, admin));
        var content = await photoUseCase.DownloadAsync(inspection.Id, photo.Id, admin);
        content.Content.Dispose();
    }

    [Fact]
    public async Task LegacyListFiltersAgencyBeforePagination_WhileSuperAdminCanReadAll()
    {
        var fake = new WorkflowFake();
        var own = await fake.DepartureAsync();
        var foreignRental = new Rental(Guid.NewGuid(), fake.Clients[0].Id, Guid.NewGuid(),
            fake.Clock.Now, Guid.NewGuid());
        var unassignedRental = new Rental(Guid.NewGuid(), fake.Clients[0].Id, Guid.NewGuid(), fake.Clock.Now);
        fake.Rentals.AddRange([foreignRental, unassignedRental]);
        foreach (var rental in new[] { foreignRental, unassignedRental })
            fake.Inspections.Insert(0, new InspectionEntity(Guid.NewGuid(), rental.Id, own.TemplateId,
                InspectionType.Departure, fake.Actor.AccountId, "Autre", fake.Clock.Now));
        var query = new QueryInspectionsUseCase(fake.Context);
        Assert.Equal(own.Id, Assert.Single(await query.ListAsync(Admin(fake), null, null, null, limit: 1)).Id);
        Assert.Empty(await query.ListAsync(Admin(fake), null, null, null, offset: 1, limit: 1));
        Assert.Equal(3, (await query.ListAsync(SuperAdmin, null, null, null)).Count);
        foreach (var rental in new[] { foreignRental, unassignedRental })
        {
            await Forbidden(() => query.GetRentalAsync(rental.Id, Admin(fake)));
            Assert.Equal(rental.Id, (await query.GetRentalAsync(rental.Id, SuperAdmin)).Id);
        }
    }

    [Fact]
    public async Task SuperAdminRetainsGlobalSearchAndExplicitAgencyFiltering()
    {
        var fake = new WorkflowFake();
        var port = new SearchPort();
        var search = new SearchInspectionsUseCase(port, fake.Context);
        await search.ExecuteAsync(new(), SuperAdmin);
        Assert.Null(port.Query!.AgencyId);
        var otherAgency = Guid.NewGuid();
        await search.ExecuteAsync(new(AgencyId: otherAgency), SuperAdmin);
        Assert.Equal(otherAgency, port.Query!.AgencyId);
        var inspection = await fake.DepartureAsync(true);
        Assert.Equal(inspection.Id, (await new QueryInspectionsUseCase(fake.Context).GetAsync(inspection.Id, SuperAdmin)).Id);
        Assert.NotEmpty(await new DownloadInspectionReportUseCase(fake.Context).ExecuteAsync(inspection.Id, SuperAdmin));
    }

    private static async Task Forbidden(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<InspectionOperationException>(action);
        Assert.Equal(403, exception.StatusCode);
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
