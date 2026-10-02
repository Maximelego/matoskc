namespace MatosKC.Application.Tests.Fakes;

using MatosKC.Application.EquipmentPhotos.Ports;
using MatosKC.Domain.Equipments;

internal sealed class FakeEquipmentPhotoRepository : IEquipmentPhotoRepository
{
    public List<EquipmentPhoto> Photos { get; } = [];

    public Task AddAsync(EquipmentPhoto photo, CancellationToken cancellationToken)
    {
        Photos.Add(photo);
        return Task.CompletedTask;
    }

    public Task<EquipmentPhoto?> RetrieveByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Photos.SingleOrDefault(photo => photo.Id == id));
    }

    public Task<IReadOnlyList<EquipmentPhoto>> ListByEquipmentIdAsync(
        Guid equipmentId,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<EquipmentPhoto> result = Photos
            .Where(photo => photo.EquipmentId == equipmentId)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(EquipmentPhoto photo, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EquipmentPhoto photo, CancellationToken cancellationToken)
    {
        Photos.Remove(photo);
        return Task.CompletedTask;
    }
}
