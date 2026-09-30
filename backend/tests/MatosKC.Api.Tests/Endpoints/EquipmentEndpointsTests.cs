namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Application.Equipments.Get;
using MatosKC.Application.Equipments.List;
using MatosKC.Domain.Equipments;

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

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new
            {
                name = "Souffleur ISOVER",
                categoryId,
                serialNumber = $"SN-{Guid.NewGuid():N}"
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotEqual(Guid.Empty, await ReadCreatedIdAsync(response));
    }

    [Fact]
    public async Task CreateEquipment_WithMalformedCategoryId_ShouldReturnBadRequest()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new
            {
                name = "Souffleur ISOVER",
                categoryId = "not-a-guid",
                serialNumber = $"SN-{Guid.NewGuid():N}"
            },
            TestContext.Current.CancellationToken
        );

        AssertProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEquipment_WithUnknownCategory_ShouldReturnNotFound()
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new
            {
                name = "Souffleur ISOVER",
                categoryId = Guid.NewGuid(),
                serialNumber = $"SN-{Guid.NewGuid():N}"
            },
            TestContext.Current.CancellationToken
        );

        AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEquipment_WithDuplicateSerialNumber_ShouldReturnConflict()
    {
        Guid categoryId = await CreateCategoryAsync();
        string serialNumber = $"SN-{Guid.NewGuid():N}";
        await CreateEquipmentAsync("First", categoryId, serialNumber);

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new { name = "Second", categoryId, serialNumber },
            TestContext.Current.CancellationToken
        );

        AssertProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateEquipment_WithBlankName_ShouldReturnBadRequest()
    {
        Guid categoryId = await CreateCategoryAsync();

        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new
            {
                name = "   ",
                categoryId,
                serialNumber = $"SN-{Guid.NewGuid():N}"
            },
            TestContext.Current.CancellationToken
        );

        AssertProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEquipment_WithExistingId_ShouldReturnItsDto()
    {
        Guid categoryId = await CreateCategoryAsync();
        string name = $"Equipment-{Guid.NewGuid():N}";
        string serialNumber = $"SN-{Guid.NewGuid():N}";
        Guid equipmentId = await CreateEquipmentAsync(name, categoryId, serialNumber);

        HttpResponseMessage response = await Client.GetAsync(
            $"/api/equipments/{equipmentId}",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        GetEquipmentDto? result = await response.Content.ReadFromJsonAsync<GetEquipmentDto>(
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(result);
        Assert.Equal(equipmentId, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(serialNumber, result.SerialNumber);
        Assert.Equal(categoryId, result.EquipmentCategoryId);
    }

    [Fact]
    public async Task GetEquipment_WithUnknownId_ShouldReturnNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync(
            $"/api/equipments/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken
        );

        AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListEquipments_ShouldReturnCreatedEquipment()
    {
        Guid categoryId = await CreateCategoryAsync();
        Guid equipmentId = await CreateEquipmentAsync(
            $"Equipment-{Guid.NewGuid():N}",
            categoryId,
            $"SN-{Guid.NewGuid():N}"
        );

        ListEquipmentResult result = await GetListAsync("/api/equipments");

        Assert.Contains(result.Equipments, equipment => equipment.Id == equipmentId);
    }

    [Fact]
    public async Task ListEquipments_WithCategoryFilter_ShouldOnlyReturnMatchingEquipment()
    {
        Guid selectedCategoryId = await CreateCategoryAsync();
        Guid otherCategoryId = await CreateCategoryAsync();
        Guid selectedId = await CreateEquipmentAsync("Selected", selectedCategoryId, $"SN-{Guid.NewGuid():N}");
        Guid excludedId = await CreateEquipmentAsync("Excluded", otherCategoryId, $"SN-{Guid.NewGuid():N}");

        ListEquipmentResult result = await GetListAsync(
            $"/api/equipments?categoryId={selectedCategoryId}"
        );

        Assert.Contains(result.Equipments, equipment => equipment.Id == selectedId);
        Assert.DoesNotContain(result.Equipments, equipment => equipment.Id == excludedId);
    }

    [Fact]
    public async Task ListEquipments_WithSearchFilter_ShouldSearchNameAndSerialNumber()
    {
        Guid categoryId = await CreateCategoryAsync();
        string marker = Guid.NewGuid().ToString("N");
        Guid byNameId = await CreateEquipmentAsync($"Machine-{marker}", categoryId, $"SN-{Guid.NewGuid():N}");
        Guid bySerialId = await CreateEquipmentAsync("Other machine", categoryId, $"SN-{marker}");

        ListEquipmentResult result = await GetListAsync(
            $"/api/equipments?search={marker}"
        );

        Assert.Contains(result.Equipments, equipment => equipment.Id == byNameId);
        Assert.Contains(result.Equipments, equipment => equipment.Id == bySerialId);
    }

    [Fact]
    public async Task ListEquipments_WithAvailableStatus_ShouldReturnCreatedEquipment()
    {
        Guid categoryId = await CreateCategoryAsync();
        Guid equipmentId = await CreateEquipmentAsync(
            "Available machine",
            categoryId,
            $"SN-{Guid.NewGuid():N}"
        );

        ListEquipmentResult result = await GetListAsync(
            $"/api/equipments?status={EquipmentStatus.Available}"
        );

        Assert.Contains(result.Equipments, equipment => equipment.Id == equipmentId);
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
        return await ReadCreatedIdAsync(response);
    }

    private async Task<Guid> CreateEquipmentAsync(
        string name,
        Guid categoryId,
        string serialNumber)
    {
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            "/api/equipments",
            new { name, categoryId, serialNumber },
            TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await ReadCreatedIdAsync(response);
    }

    private static async Task<Guid> ReadCreatedIdAsync(HttpResponseMessage response)
    {
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<ListEquipmentResult> GetListAsync(string uri)
    {
        HttpResponseMessage response = await Client.GetAsync(
            uri,
            TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        ListEquipmentResult? result = await response.Content.ReadFromJsonAsync<ListEquipmentResult>(
            TestContext.Current.CancellationToken
        );
        return Assert.IsType<ListEquipmentResult>(result);
    }

    private static void AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
