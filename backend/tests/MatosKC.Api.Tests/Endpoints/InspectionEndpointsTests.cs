namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(ApiTestCollection.Name)]
public sealed class InspectionEndpointsTests(MatosKCApiFactory factory)
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="
    );

    private async Task<(Guid EquipmentId, Guid CategoryId, int AgencyCode)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
        var category = new MatosKC.Domain.Equipments.EquipmentCategory("Test " + Guid.NewGuid(), null);
        var equipment = new MatosKC.Domain.Equipments.Equipment("Camion", category.Id, Guid.NewGuid().ToString());
        var agency = new Agency("Test location", Random.Shared.Next(100000, int.MaxValue));
        db.AddRange(category, equipment, agency);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (equipment.Id, category.Id, agency.Code);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    private static Task<HttpResponseMessage> TemplateAsync(HttpClient client, Guid categoryId) =>
        client.PostAsJsonAsync("/inspection-templates", new
        {
            equipmentCategoryId = categoryId,
            points = new[] { new { description = "Etat extérieur", photoId = (Guid?)null } }
        }, cancellationToken: TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> DepartureAsync(HttpClient client, Guid equipmentId, int agencyCode) =>
        client.PostAsJsonAsync("/inspections/departure", new
        {
            equipmentId,
            operatorFirstName = "Maxime",
            client = new { displayName = "Client", email = "client@example.test", phone = "0612345678", agencyCode, commandValue = "001234" }
        }, cancellationToken: TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, Guid inspectionId, Guid pointId, bool defective) =>
        client.PutAsJsonAsync($"/inspections/{inspectionId}/draft", new
        {
            operatorFirstName = "Maxime",
            answers = new[] { new { templatePointId = pointId, result = defective ? "NonCompliant" : "Compliant", observation = defective ? "Défaut" : null } }
        }, cancellationToken: TestContext.Current.CancellationToken);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DepartureAndReturn_PersistStatePhotosAndUseLatestTemplate(bool defective)
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        var template = await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var departure = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid departureId = departure.GetProperty("id").GetGuid();
        Guid rentalId = departure.GetProperty("rentalId").GetGuid();
        Guid pointId = template.GetProperty("points")[0].GetProperty("id").GetGuid();
        await JsonAsync(await SaveAsync(client, departureId, pointId, false));
        var validatedDeparture = await JsonAsync(await client.PostAsync($"/inspections/{departureId}/validation", null, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Validated", validatedDeparture.GetProperty("status").GetString());
        var equipment = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Borrowed", equipment.GetProperty("status").GetString());

        var latest = await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var returned = await JsonAsync(await client.PostAsJsonAsync($"/rentals/{rentalId}/return-inspection", new { operatorFirstName = "Maxime" }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(latest.GetProperty("id").GetGuid(), returned.GetProperty("templateId").GetGuid());
        Guid returnId = returned.GetProperty("id").GetGuid();
        var saved = await JsonAsync(await SaveAsync(client, returnId, latest.GetProperty("points")[0].GetProperty("id").GetGuid(), defective));

        if (defective)
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity,
                (await client.PostAsync($"/inspections/{returnId}/validation", null, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
            using var form = new MultipartFormDataContent();
            var image = new ByteArrayContent(Png);
            image.Headers.ContentType = new("image/png");
            form.Add(image, "file", "photo.png");
            Guid answerId = saved.GetProperty("answers")[0].GetProperty("id").GetGuid();
            var photo = await JsonAsync(await client.PostAsync($"/inspections/{returnId}/answers/{answerId}/photos", form, cancellationToken: TestContext.Current.CancellationToken));
            Guid photoId = photo.GetProperty("id").GetGuid();
            Assert.Equal(Png, await client.GetByteArrayAsync($"/inspections/{returnId}/photos/{photoId}", cancellationToken: TestContext.Current.CancellationToken));
            saved = await JsonAsync(await SaveAsync(client, returnId, latest.GetProperty("points")[0].GetProperty("id").GetGuid(), true));
            Assert.Equal(photoId, saved.GetProperty("answers")[0].GetProperty("photoIds")[0].GetGuid());
        }

        var validatedReturn = await JsonAsync(await client.PostAsync($"/inspections/{returnId}/validation", null, cancellationToken: TestContext.Current.CancellationToken));
        var retry = await JsonAsync(await client.PostAsync($"/inspections/{returnId}/validation", null, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(validatedReturn.GetProperty("validatedAt").GetString(), retry.GetProperty("validatedAt").GetString());
        equipment = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(defective ? "ToBeDecided" : "Available", equipment.GetProperty("status").GetString());
        var rental = await JsonAsync(await client.GetAsync($"/rentals/{rentalId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Completed", rental.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await SaveAsync(client, returnId, latest.GetProperty("points")[0].GetProperty("id").GetGuid(), false)).StatusCode);

        if (defective)
        {
            Assert.Equal(HttpStatusCode.Conflict,
                (await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode)).StatusCode);
            await JsonAsync(await client.PatchAsJsonAsync($"/equipments/{seed.EquipmentId}/status", new { status = "Available" }, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.Created,
                (await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode)).StatusCode);
        }
    }

    [Fact]
    public async Task AgencyList_IsPublicAndAdministrationRemainsProtected()
    {
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var result = await JsonAsync(await anonymous.GetAsync("/agencies", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(JsonValueKind.Array, result.GetProperty("agencies").ValueKind);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.GetAsync("/agencies/11111111-1111-1111-1111-111111111111", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanReadButCannotPerformInspections()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/inspections", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.PostAsJsonAsync("/inspections/departure", new { }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Agency_CannotListAllInspectOtherAgencyOrManageEquipment()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        await JsonAsync(await TemplateAsync(admin, seed.CategoryId));
        var departure = await JsonAsync(await DepartureAsync(admin, seed.EquipmentId, seed.AgencyCode));
        using var agency = await factory.CreateAuthenticatedClientAsync(Role.Agency);
        Assert.Equal(HttpStatusCode.Forbidden, (await agency.GetAsync("/inspections", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await agency.GetAsync($"/inspections/{departure.GetProperty("id").GetGuid()}", cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await agency.PatchAsJsonAsync($"/equipments/{seed.EquipmentId}/status", new { status = "Decommissioned" }, cancellationToken: TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task DraftWrite_RequiresCsrfAndRejectsForeignPoints()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var inspection = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = inspection.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await SaveAsync(client, id, Guid.NewGuid(), false)).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var rejected = await client.PostAsync($"/inspections/{id}/validation", null, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        using var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Invalid CSRF token", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ConcurrentDepartureCreation_AllowsOnlyOneOpenRental()
    {
        using var first = await factory.CreateAuthenticatedClientAsync();
        using var second = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        await JsonAsync(await TemplateAsync(first, seed.CategoryId));
        var responses = await Task.WhenAll(
            DepartureAsync(first, seed.EquipmentId, seed.AgencyCode),
            DepartureAsync(second, seed.EquipmentId, seed.AgencyCode)
        );
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
    }
}
