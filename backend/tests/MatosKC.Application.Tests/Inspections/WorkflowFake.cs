namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Rentals.Ports;
using MatosKC.Application.Persistence;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Agencies.List;
using MatosKC.Application.Tests.Fakes;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

internal sealed class WorkflowFake : IClientRepository, IRentalRepository, IInspectionsRepository,
    IInspectionTemplateRepository, IInspectionMediaRepository, IUnitOfWork, IAgencyRepository,
    IInspectionReportGenerator, IInspectionSignatureValidator, IInspectionTemplatePhotoRepository
{
    internal List<Client> Clients { get; } = [];
    internal List<Rental> Rentals { get; } = [];
    internal List<InspectionEntity> Inspections { get; } = [];
    internal List<InspectionTemplate> Templates { get; } = [];
    internal List<InspectionTemplatePhoto> TemplatePhotos { get; } = [];
    internal FakeImageContentValidator ImageValidator { get; } = new();
    internal List<InspectionMedia> Media { get; } = [];
    internal FakeEquipmentRepository EquipmentRepository { get; } = new();
    internal FakeEquipmentCategoryRepository Categories { get; } = new();
    internal FakeObjectStorage Storage { get; } = new();
    internal Agency Agency { get; } = new("Epinal", 83);
    internal Equipment Equipment { get; }
    internal InspectionActor Actor { get; }
    internal int Commits { get; private set; }
    internal bool FailCommit { get; set; }
    internal FixedClock Clock { get; } = new();
    internal InspectionWorkflowContext Context { get; }

    internal WorkflowFake()
    {
        Actor = new InspectionActor(Guid.NewGuid(), Role.Agency, 83, Agency.Id);
        var category = new EquipmentCategory("Camion", null);
        Categories.Seed(category);
        Equipment = new Equipment("Camion", category.Id, Guid.NewGuid().ToString());
        EquipmentRepository.AddedEquipments.Add(Equipment);
        AddTemplate();
        Context = new(this, this, this, this, this, EquipmentRepository, Categories,
            this, Storage, this, Clock, this, this, this, ImageValidator);
    }

    internal InspectionTemplate AddTemplate()
    {
        Guid id = Guid.NewGuid();
        var template = new InspectionTemplate(id,
            [new InspectionTemplatePoint(Guid.NewGuid(), id, "Etat extérieur", "Merci de vérifier la présence de...", null)], Equipment.CategoryId);
        Templates.Add(template);

        return template;
    }

    internal CreateDepartureRequest Request => new(Equipment.Id,
        new ClientInput("Client", "client@example.test", "0612345678", 83, "001234"), "Maxime");

    internal async Task<InspectionEntity> DepartureAsync(bool validate = false)
    {
        var response = await new CreateDepartureInspectionUseCase(Context).ExecuteAsync(Request, Actor);
        var inspection = Inspections.Single(value => value.Id == response.Id);
        inspection.SetAnswer(Templates[0].Points[0].Id, InspectionResult.Compliant, null);

        inspection.Sign("Client", [1], Clock.Now);

        if (validate)
        {
            await new ValidateDepartureInspectionUseCase(Context).ExecuteAsync(inspection.Id, Actor);
        }

        return inspection;
    }

    public void Validate(byte[] png) { }

    public Task<byte[]> GenerateAsync(InspectionEntity inspection, Rental rental, InspectionTemplate template,
        CancellationToken ct = default) => Task.FromResult<byte[]>([37, 80, 68, 70]);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await operation(ct);

        if (FailCommit)
        {
            throw new InvalidOperationException("Simulated commit failure.");
        }

        Commits++;
        return result;
    }

    Task<Client?> IClientRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Clients.SingleOrDefault(value => value.Id == id));

    public Task AddClientAsync(Client client, CancellationToken ct = default)
    {
        Clients.Add(client);
        return Task.CompletedTask;
    }

    Task<Rental?> IRentalRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Rentals.SingleOrDefault(value => value.Id == id));

    public Task<Rental?> GetActiveForEquipmentAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Rentals.SingleOrDefault(value => value.EquipmentId == id && value.Status == RentalStatus.Active));

    public Task<Rental?> GetOpenForEquipmentAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Rentals.SingleOrDefault(value => value.EquipmentId == id
            && (value.Status == RentalStatus.Draft || value.Status == RentalStatus.Active)));

    public Task AddAsync(Rental rental, CancellationToken ct = default)
    {
        Rentals.Add(rental);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Rental rental, CancellationToken ct = default) => Task.CompletedTask;

    public Task<InspectionEntity?> GetInspectionByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Inspections.SingleOrDefault(value => value.Id == id));

    public Task<InspectionEntity?> GetForRentalAsync(Guid id, InspectionType type, CancellationToken ct = default) =>
        Task.FromResult(Inspections.SingleOrDefault(value => value.RentalId == id && value.Type == type && value.Status != InspectionStatus.Cancelled));

    public Task AddInspectionAsync(InspectionEntity inspection, CancellationToken ct = default)
    {
        Inspections.Add(inspection);
        return Task.CompletedTask;
    }

    public Task UpdateInspectionAsync(InspectionEntity inspection, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<InspectionEntity>> ListAsync(Guid? id, InspectionType? type, InspectionStatus? status,
        int offset, int limit, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<InspectionEntity>>(
            Inspections.Where(value => (!id.HasValue || value.RentalId == id)
                && (!type.HasValue || value.Type == type) && (!status.HasValue || value.Status == status))
                .Skip(offset).Take(limit).ToArray());

    public Task<InspectionTemplate?> GetTemplateByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Templates.SingleOrDefault(value => value.Id == id));

    public Task<InspectionTemplate?> GetLatestForCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        Task.FromResult(Templates.LastOrDefault(value => value.EquipmentCategoryId == categoryId));

    public Task AddAsync(InspectionTemplate template, CancellationToken ct = default)
    {
        Templates.Add(template);
        return Task.CompletedTask;
    }

    Task<InspectionMedia?> IInspectionMediaRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Media.SingleOrDefault(value => value.Id == id));

    Task<InspectionTemplatePhoto?> IInspectionTemplatePhotoRepository.GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(TemplatePhotos.SingleOrDefault(photo => photo.Id == id));

    public Task AddAsync(InspectionTemplatePhoto photo, CancellationToken ct = default)
    {
        TemplatePhotos.Add(photo);
        return Task.CompletedTask;
    }

    public Task AddAsync(InspectionMedia media, CancellationToken ct = default)
    {
        Media.Add(media);
        return Task.CompletedTask;
    }

    public Task<Agency?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult<Agency?>(id == Agency.Id ? Agency : null);

    public Task<Agency?> GetByCodeAsync(int code, CancellationToken ct = default) =>
        Task.FromResult<Agency?>(code == Agency.Code ? Agency : null);

    public Task<bool> ExistsByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(id == Agency.Id);

    public Task<Agency[]?> ListByQueryAsync(ListAgenciesQuery query, CancellationToken ct = default) =>
        Task.FromResult<Agency[]?>([Agency]);

    public Task<Agency> AddAsync(Agency agency, CancellationToken ct = default) => Task.FromResult(agency);

    internal sealed class FixedClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
