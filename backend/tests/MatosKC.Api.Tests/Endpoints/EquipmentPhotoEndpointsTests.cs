namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

[Collection(ApiTestCollection.Name)]
public sealed class EquipmentPhotoEndpointsTests
{
    private readonly HttpClient Client;

    public EquipmentPhotoEndpointsTests(MatosKCApiFactory factory)
    {
        Client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task PhotoLifecycle_ShouldUploadListDownloadReplaceAndDelete()
    {
        Guid equipmentId = await CreateEquipmentAsync();
        Guid photoId = await UploadAsync(equipmentId, [1, 2, 3], "first.png", "image/png");

        JsonElement[] photos = await Client.GetFromJsonAsync<JsonElement[]>(
            $"/api/equipments/{equipmentId}/photos",
            TestContext.Current.CancellationToken
        ) ?? [];
        Assert.Contains(photos, photo => photo.GetProperty("id").GetGuid() == photoId);

        byte[] downloaded = await Client.GetByteArrayAsync(
            $"/api/equipments/{equipmentId}/photos/{photoId}",
            TestContext.Current.CancellationToken
        );
        Assert.Equal([1, 2, 3], downloaded);

        using var putContent = CreateMultipart([7, 8], "replacement.webp", "image/webp");
        HttpResponseMessage putResponse = await Client.PutAsync(
            $"/api/equipments/{equipmentId}/photos/{photoId}",
            putContent,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        HttpResponseMessage deleteResponse = await Client.DeleteAsync(
            $"/api/equipments/{equipmentId}/photos/{photoId}",
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage missingResponse = await Client.GetAsync(
            $"/api/equipments/{equipmentId}/photos/{photoId}",
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Upload_WithUnsupportedContentType_ShouldReturnBadRequest()
    {
        Guid equipmentId = await CreateEquipmentAsync();
        using var content = CreateMultipart([1], "notes.txt", "text/plain");

        HttpResponseMessage response = await Client.PostAsync(
            $"/api/equipments/{equipmentId}/photos",
            content,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Upload_ForUnknownEquipment_ShouldReturnNotFound()
    {
        using var content = CreateMultipart([1], "photo.png", "image/png");

        HttpResponseMessage response = await Client.PostAsync(
            $"/api/equipments/{Guid.NewGuid()}/photos",
            content,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> UploadAsync(
        Guid equipmentId,
        byte[] bytes,
        string fileName,
        string contentType
    )
    {
        using var content = CreateMultipart(bytes, fileName, contentType);
        HttpResponseMessage response = await Client.PostAsync(
            $"/api/equipments/{equipmentId}/photos",
            content,
            TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateEquipmentAsync()
    {
        HttpResponseMessage categoryResponse = await Client.PostAsJsonAsync(
            "/api/equipment-categories",
            new { name = $"Photos-{Guid.NewGuid():N}", description = (string?)null },
            TestContext.Current.CancellationToken
        );
        categoryResponse.EnsureSuccessStatusCode();
        using JsonDocument categoryBody = JsonDocument.Parse(
            await categoryResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        Guid categoryId = categoryBody.RootElement.GetProperty("id").GetGuid();

        HttpResponseMessage equipmentResponse = await Client.PostAsJsonAsync(
            "/api/equipments",
            new
            {
                name = "Equipment with photos",
                categoryId,
                serialNumber = $"PHOTO-{Guid.NewGuid():N}"
            },
            TestContext.Current.CancellationToken
        );
        equipmentResponse.EnsureSuccessStatusCode();
        using JsonDocument equipmentBody = JsonDocument.Parse(
            await equipmentResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        return equipmentBody.RootElement.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent CreateMultipart(
        byte[] bytes,
        string fileName,
        string contentType
    )
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", fileName);
        return multipart;
    }
}
