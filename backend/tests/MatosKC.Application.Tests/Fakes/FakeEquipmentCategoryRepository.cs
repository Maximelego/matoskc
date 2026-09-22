using MatosKC.Application.EquipmentCategories.Ports;
using MatosKC.Domain.Equipments;

namespace MatosKC.Application.Tests.Fakes;

internal sealed class FakeEquipmentCategoryRepository : IEquipmentCategoryRepository
{
    private readonly Dictionary<Guid, EquipmentCategory> _categories = [];

    public bool NameAlreadyExists { get; set; }

    public List<EquipmentCategory> AddedCategories { get; } = [];

    public string? LastCheckedName { get; private set; }

    public Guid? LastCheckedId { get; private set; }

    public Guid? LastRequestedId { get; private set; }

    public CancellationToken ExistsByNameCancellationToken { get; private set; }

    public CancellationToken ExistsByIdCancellationToken { get; private set; }

    public CancellationToken GetByIdCancellationToken { get; private set; }

    public CancellationToken AddCancellationToken { get; private set; }

    public void Seed(EquipmentCategory category)
    {
        _categories.Add(category.Id, category);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        LastCheckedName = name;
        ExistsByNameCancellationToken = cancellationToken;

        return Task.FromResult(NameAlreadyExists);
    }

    public Task<bool> ExistsByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LastCheckedId = id;
        ExistsByIdCancellationToken = cancellationToken;

        return Task.FromResult(_categories.ContainsKey(id));
    }

    public Task<EquipmentCategory?> GetEquipmentCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        LastRequestedId = id;
        GetByIdCancellationToken = cancellationToken;
        _categories.TryGetValue(id, out var category);

        return Task.FromResult(category);
    }

    public Task AddAsync(
        EquipmentCategory category,
        CancellationToken cancellationToken = default)
    {
        AddedCategories.Add(category);
        AddCancellationToken = cancellationToken;

        return Task.CompletedTask;
    }
}
