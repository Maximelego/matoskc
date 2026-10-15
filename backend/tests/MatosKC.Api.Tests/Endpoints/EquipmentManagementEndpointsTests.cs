namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

[Collection(ApiTestCollection.Name)]
public sealed class EquipmentManagementEndpointsTests(MatosKCApiFactory factory)
{
    private async Task<(Guid EquipmentId, Guid CategoryId)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
        var category = new EquipmentCategory("Category " + Guid.NewGuid(), null);
        var equipment = new Equipment("Original", category.Id, Guid.NewGuid().ToString());
        db.AddRange(category, equipment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (equipment.Id, category.Id);
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    [Fact]
    public async Task NoCurrentRental_ReturnsExplicitJsonEnvelope()
    {
        var seed = await SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();
        var json = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}/current-rental", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(JsonValueKind.Null, json.GetProperty("rental").ValueKind);
    }

    [Fact]
    public async Task AdministratorCanRenameAndDecideStatus_WithAuditedReasonAndActor()
    {
        var seed = await SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        var account = await JsonAsync(await client.GetAsync("/auth/me", cancellationToken: TestContext.Current.CancellationToken));
        await JsonAsync(await client.PutAsJsonAsync($"/equipments/{seed.EquipmentId}",
            new { name = "Renamed", equipmentCategoryId = seed.CategoryId }, cancellationToken: TestContext.Current.CancellationToken));
        await JsonAsync(await client.PutAsJsonAsync($"/equipment-categories/{seed.CategoryId}",
            new { name = "Updated " + Guid.NewGuid(), description = "Multiline\nDetails" }, cancellationToken: TestContext.Current.CancellationToken));

        using var missingReason = await client.PatchAsJsonAsync($"/equipments/{seed.EquipmentId}/status",
            new { status = "Decommissioned" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, missingReason.StatusCode);

        await JsonAsync(await client.PatchAsJsonAsync($"/equipments/{seed.EquipmentId}/status",
            new { status = "Decommissioned", reason = "Retrait définitif après examen." }, cancellationToken: TestContext.Current.CancellationToken));
        var history = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}/history", cancellationToken: TestContext.Current.CancellationToken));
        var decision = history.EnumerateArray().Single(entry =>
            entry.GetProperty("reason").GetString() == "Retrait définitif après examen.");
        Assert.Equal("Modified", decision.GetProperty("operation").GetString());
        Assert.NotEqual(Guid.Empty, decision.GetProperty("accountId").GetGuid());
        Assert.Contains("Decommissioned", decision.GetProperty("changesJson").GetString());

        var equipment = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Renamed", equipment.GetProperty("name").GetString());
    }

    [Fact]
    public async Task AgencyCannotEditOrReadEquipmentHistory()
    {
        var seed = await SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync(Role.Agency);
        using var edit = await client.PutAsJsonAsync($"/equipments/{seed.EquipmentId}",
            new { name = "Denied", equipmentCategoryId = seed.CategoryId }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        using var history = await client.GetAsync($"/equipments/{seed.EquipmentId}/history", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
    }

    [Fact]
    public async Task UploadedPhoto_IsExposedAsListThumbnail_AndReplacementChangesUrl()
    {
        var seed = await SeedAsync();
        using var client = await factory.CreateAuthenticatedClientAsync();
        byte[] png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAACgAAAAeCAIAAADRv8uKAAAALklEQVR4nO3NMQEAMAgAoLlIJjG2sazg5wMFiM56F/7JKhaLxWKxWCwWi8XilQH93wGGpwUmBgAAAABJRU5ErkJggg==");

        MultipartFormDataContent Form()
        {
            var form = new MultipartFormDataContent();
            var bytes = new ByteArrayContent(png);
            bytes.Headers.ContentType = new("image/png");
            form.Add(bytes, "file", "photo.png");
            return form;
        }

        using var upload = Form();
        var photo = await JsonAsync(await client.PostAsync($"/equipments/{seed.EquipmentId}/photos", upload, cancellationToken: TestContext.Current.CancellationToken));
        Guid photoId = photo.GetProperty("id").GetGuid();
        var list = await JsonAsync(await client.GetAsync($"/equipments?categoryId={seed.CategoryId}", cancellationToken: TestContext.Current.CancellationToken));
        var equipment = list.GetProperty("equipments").EnumerateArray().Single();
        Assert.Equal(photoId, equipment.GetProperty("primaryPhotoId").GetGuid());
        string url = equipment.GetProperty("thumbnailUrl").GetString()!;
        Assert.Equal(png, await client.GetByteArrayAsync(url, cancellationToken: TestContext.Current.CancellationToken));

        using var replacement = Form();
        await JsonAsync(await client.PutAsync($"/equipments/{seed.EquipmentId}/photos/{photoId}", replacement, cancellationToken: TestContext.Current.CancellationToken));
        var nextList = await JsonAsync(await client.GetAsync($"/equipments?categoryId={seed.CategoryId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.NotEqual(url, nextList.GetProperty("equipments")[0].GetProperty("thumbnailUrl").GetString());
    }
}
