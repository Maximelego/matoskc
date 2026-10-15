namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection(ApiTestCollection.Name)]
public sealed class EquipmentCareEndpointsTests(MatosKCApiFactory factory)
{
    private async Task<Guid> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
        var category = new EquipmentCategory("Care " + Guid.NewGuid(), null);
        var equipment = new Equipment("Machine", category.Id, Guid.NewGuid().ToString());
        db.AddRange(category, equipment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return equipment.Id;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task PreventiveMaintenance_IsPersistentIdempotentAndReadableByAgency()
    {
        Guid equipmentId = await SeedAsync();
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var agency = await factory.CreateAuthenticatedClientAsync(Role.Agency);
        Guid id = Guid.NewGuid();
        var request = new { id, performedAt = DateTimeOffset.UtcNow.AddMinutes(-1).AddTicks(1),
            performedBy = "Atelier", workDescription = "Graissage préventif", resolvedDefectIds = Array.Empty<Guid>() };
        var first = await JsonAsync(await admin.PostAsJsonAsync($"/equipments/{equipmentId}/maintenances", request,
            TestContext.Current.CancellationToken));
        var retry = await JsonAsync(await admin.PostAsJsonAsync($"/equipments/{equipmentId}/maintenances", request,
            TestContext.Current.CancellationToken));
        Assert.Equal(first.GetProperty("recordedAt").GetDateTimeOffset(), retry.GetProperty("recordedAt").GetDateTimeOffset(),
            TimeSpan.FromMicroseconds(1));
        var list = await JsonAsync(await agency.GetAsync($"/equipments/{equipmentId}/maintenances", TestContext.Current.CancellationToken));
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(id, list.GetProperty("items")[0].GetProperty("id").GetGuid());
        var detail = await JsonAsync(await agency.GetAsync($"/equipments/{equipmentId}/maintenances/{id}", TestContext.Current.CancellationToken));
        Assert.Equal("Graissage préventif", detail.GetProperty("workDescription").GetString());

        var equipment = await JsonAsync(await admin.GetAsync($"/equipments/{equipmentId}", TestContext.Current.CancellationToken));
        Assert.Equal("Available", equipment.GetProperty("status").GetString());
        using var forbidden = await agency.PostAsJsonAsync($"/equipments/{equipmentId}/maintenances", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var conflict = await admin.PostAsJsonAsync($"/equipments/{equipmentId}/maintenances",
            request with { workDescription = "Different" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task InvalidOrForeignRequests_AreRejected()
    {
        Guid equipmentId = await SeedAsync();
        using var admin = await factory.CreateAuthenticatedClientAsync();
        var request = new { id = Guid.NewGuid(), performedAt = DateTimeOffset.UtcNow.AddDays(1),
            performedBy = "Atelier", workDescription = "Réparation", resolvedDefectIds = Array.Empty<Guid>() };
        using var future = await admin.PostAsJsonAsync($"/equipments/{equipmentId}/maintenances", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        using var invalid = await admin.GetAsync($"/equipments/{equipmentId}/defects?limit=101", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var missing = await admin.GetAsync($"/equipments/{equipmentId}/maintenances/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
