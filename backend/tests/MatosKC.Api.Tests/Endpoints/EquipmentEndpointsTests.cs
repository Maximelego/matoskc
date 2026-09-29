namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

[Collection(ApiTestCollection.Name)]
public sealed class EquipmentEndpointsTests
{
    private readonly HttpClient Client;

    public EquipmentEndpointsTests(MatosKCApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateEquipment_WithExistingCategory_ShouldReturnCreated()
    {
        Guid categoryId = await CreateCategoryAsync();
        var request = new
        {
            name = "Souffleur ISOVER",
            categoryId,
            serialNumber = $"SN-{Guid.NewGuid():N}"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            request,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            )
        );

        Assert.NotEqual(Guid.Empty, body.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task CreateEquipment_WithMalformedCategoryId_ShouldReturnBadRequest()
    {
        var request = new
        {
            name = "Souffleur ISOVER",
            categoryId = "not-a-guid",
            serialNumber = $"SN-{Guid.NewGuid():N}"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            request,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
    }

    private async Task<Guid> CreateCategoryAsync()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipment-categories",
            new
            {
                name = $"Category-{Guid.NewGuid():N}",
                description = (string?)null
            },
            TestContext.Current.CancellationToken
        );

        response.EnsureSuccessStatusCode();

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            )
        );

        return body.RootElement.GetProperty("id").GetGuid();
    }
}
