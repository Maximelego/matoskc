namespace MatosKC.Application.Clients.Ports;

using MatosKC.Domain.Entities.Clients;

public interface IClientRepository
{
    public Task<Client?> GetByIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    public Task AddClientAsync(Client client, CancellationToken cancellationToken = default);
}
