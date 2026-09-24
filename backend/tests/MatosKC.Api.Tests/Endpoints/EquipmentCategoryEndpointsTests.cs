namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

[Collection(ApiTestCollection.Name)]
public sealed class EquipmentCategoryEndpointsTests
{
    private readonly HttpClient Client;

    public EquipmentCategoryEndpointsTests(MatosKCApiFactory factory)
    {
        Client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCategory_WithValidRequest_ShouldReturnCreated()
    {
        var request = new
        {
            name = $"Fenwick-{Guid.NewGuid():N}",
            description = "Matériel de manutention"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipment-categories",
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

        Guid id = body.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task GetCategory_WithExistingId_ShouldReturnItsDto()
    {
        string name = $"Souffleur-{Guid.NewGuid():N}";
        const string description = "Souffleur de laine isolante";
        Guid id = await CreateCategoryAsync(name, description);

        HttpResponseMessage response = await Client.GetAsync(
            $"/api/equipment-categories/{id}",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal(id, body.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(name, body.RootElement.GetProperty("name").GetString());
        Assert.Equal(
            description,
            body.RootElement.GetProperty("description").GetString()
        );
    }

    [Fact]
    public async Task GetCategory_WithUnknownId_ShouldReturnNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync(
            $"/api/equipment-categories/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
    }

    [Fact]
    public async Task CreateCategory_WithBlankName_ShouldReturnBadRequest()
    {
        var request = new
        {
            name = "   ",
            description = (string?)null
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipment-categories",
            request,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
    }

    [Fact]
    public async Task ListCategories_ShouldReturnAllCategories()
    {
        string name1 = $"Category-{Guid.NewGuid():N}";
        string name2 = $"Category-{Guid.NewGuid():N}";

        await CreateCategoryAsync(name1, null);
        await CreateCategoryAsync(name2, null);

        HttpResponseMessage response = await Client.GetAsync(
            "/api/equipment-categories",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken
            )
        );

        var categories = body.RootElement.EnumerateArray()
            .Select(element => new
            {
                Id = element.GetProperty("id").GetGuid(),
                Name = element.GetProperty("name").GetString()
            })
            .ToList();

        Assert.Contains(categories, c => c.Name == name1);
        Assert.Contains(categories, c => c.Name == name2);
    }

    private async Task<Guid> CreateCategoryAsync(
        string name,
        string? description
    )
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipment-categories",
            new { name, description },
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
