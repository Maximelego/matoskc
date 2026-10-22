namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Persistence;
using MatosKC.Application.Agencies;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Infrastructure.Hash;
using MatosKC.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Real routing, cookies, claims and EF repositories; no Docker or external storage.
public sealed class AgencyResourceIsolationTests
{
    [Fact]
    public async Task ApiScopesCategoriesEquipmentTemplatesRentalsAndMaintenance_AndRejectsForgedOwnership()
    {
        await using var factory = new Factory();
        using var admin83 = await factory.Login("admin83@example.test");
        using var admin33 = await factory.Login("admin33@example.test");
        using var super = await factory.Login("super@example.test");
        var agency83 = Factory.Agency83; var agency33 = Factory.Agency33;
        var cat83 = (await Json(await admin83.PostAsJsonAsync("/equipment-categories", new { name = "Machines", description = "83" }))).GetProperty("id").GetGuid();
        var cat33 = (await Json(await admin33.PostAsJsonAsync("/equipment-categories", new { name = "Machines", description = "33" }))).GetProperty("id").GetGuid();
        Assert.NotEqual(cat83, cat33);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin83.PostAsJsonAsync("/equipment-categories", new { name = "Injection", agencyId = agency33 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await super.PostAsJsonAsync("/equipment-categories", new { name = "Missing owner" })).StatusCode);
        static MultipartFormDataContent Photo()
        {
            var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAACgAAAAeCAIAAADRv8uKAAAALklEQVR4nO3NMQEAMAgAoLlIJjG2sazg5wMFiM56F/7JKhaLxWKxWCwWi8XilQH93wGGpwUmBgAAAABJRU5ErkJggg=="));
            file.Headers.ContentType = new("image/png"); form.Add(file, "file", "guide.png"); return form;
        }
        using var ownPhoto = Photo();
        var illustration = await Json(await admin83.PostAsync("/inspection-templates/photos", ownPhoto));
        var photoId = illustration.GetProperty("id").GetGuid();
        Assert.Equal(agency83, illustration.GetProperty("agencyId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await admin33.GetAsync($"/inspection-templates/photos/{photoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin83.GetAsync($"/inspection-templates/photos/{photoId}")).StatusCode);
        using var unownedPhoto = Photo();
        Assert.Equal(HttpStatusCode.BadRequest, (await super.PostAsync("/inspection-templates/photos", unownedPhoto)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await super.PostAsJsonAsync("/inspection-templates", new
        { equipmentCategoryId = cat33, points = new[] { new { description = "Wrong agency", descriptionDetail = "Contrôle", photoId } } })).StatusCode);
        var eq83 = (await Json(await admin83.PostAsJsonAsync("/equipments", new { name = "Machine 83", categoryId = cat83, serialNumber = "S83" }))).GetProperty("id").GetGuid();
        var eq33 = (await Json(await admin33.PostAsJsonAsync("/equipments", new { name = "Machine 33", categoryId = cat33, serialNumber = "S33" }))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await admin83.PostAsJsonAsync("/equipments", new { name = "Injection", categoryId = cat33, serialNumber = "BAD" })).StatusCode);
        var equipments = await Json(await admin83.GetAsync("/equipments"));
        Assert.Single(equipments.GetProperty("equipments").EnumerateArray());
        Assert.Equal(agency83, equipments.GetProperty("equipments")[0].GetProperty("agencyId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await admin83.GetAsync($"/equipments?agencyId={agency33}")).StatusCode);
        var all = await Json(await super.GetAsync("/equipments"));
        Assert.Equal(2, all.GetProperty("equipments").GetArrayLength());
        var selected = await Json(await super.GetAsync($"/equipments?agencyId={agency33}"));
        Assert.Single(selected.GetProperty("equipments").EnumerateArray());
        async Task<JsonElement> Template(HttpClient client, Guid category) => await Json(await client.PostAsJsonAsync("/inspection-templates", new
        { equipmentCategoryId = category, points = new[] { new { description = "Etat", descriptionDetail = "Contrôle", photoId = (Guid?)null } } }));
        var template83 = await Template(admin83, cat83); var template33 = await Template(admin33, cat33);
        Assert.Equal(agency83, template83.GetProperty("agencyId").GetGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await admin83.GetAsync($"/inspection-templates/{template33.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin83.GetAsync($"/equipment-categories/{cat33}/inspection-template")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await super.PutAsJsonAsync($"/equipments/{eq83}", new { name = "Move", equipmentCategoryId = cat33 })).StatusCode);
        foreach (var path in new[] { $"/equipments/{eq33}", $"/equipments/{eq33}/photos", $"/equipments/{eq33}/history",
            $"/equipments/{eq33}/defects", $"/equipments/{eq33}/maintenances", $"/equipment-categories/{cat33}" })
            Assert.Equal(HttpStatusCode.NotFound, (await admin83.GetAsync(path)).StatusCode);
        var maintenance = await Json(await admin33.PostAsJsonAsync($"/equipments/{eq33}/maintenances", new
        { id = Guid.NewGuid(), performedAt = DateTimeOffset.UtcNow.AddSeconds(-1), performedBy = "Atelier", workDescription = "Contrôle", resolvedDefectIds = Array.Empty<Guid>() }));
        Assert.Equal(HttpStatusCode.NotFound, (await admin83.GetAsync($"/equipments/{eq33}/maintenances/{maintenance.GetProperty("id").GetGuid()}")).StatusCode);
        async Task<HttpResponseMessage> Departure(Guid equipment, int code) => await super.PostAsJsonAsync("/inspections/departure", new
        { equipmentId = equipment, operatorFirstName = "Maxime", client = new { displayName = "Client", email = "c@example.test", phone = "0612345678", agencyCode = code, commandValue = "001234" } });
        Assert.Equal(HttpStatusCode.Conflict, (await Departure(eq83, 33)).StatusCode);
        var departure = await Json(await Departure(eq83, 83));
        Assert.Equal(HttpStatusCode.NotFound, (await admin33.GetAsync($"/inspections/{departure.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin33.GetAsync($"/rentals/{departure.GetProperty("rentalId").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin83.GetAsync($"/inspections/{departure.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await super.GetAsync($"/inspections/{departure.GetProperty("id").GetGuid()}")).StatusCode);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using (response) { response.EnsureSuccessStatusCode(); using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return document.RootElement.Clone(); }
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        public static readonly Guid Agency83 = Guid.Parse("83000000-0000-0000-0000-000000000083");
        public static readonly Guid Agency33 = Guid.Parse("33000000-0000-0000-0000-000000000033");
        private readonly string database = Guid.NewGuid().ToString();
        private readonly InMemoryDatabaseRoot root = new();
        private bool seeded;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:MatosKCDatabase", "Host=unused;Database=unused;Username=unused;Password=unused");
            builder.UseSetting("S3:ServiceUrl", "http://unused.test"); builder.UseSetting("S3:Region", "us-east-1");
            builder.UseSetting("S3:AccessKey", "test"); builder.UseSetting("S3:SecretKey", "test"); builder.UseSetting("S3:BucketName", "test");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<MatosKCDbContext>(); services.RemoveAll<DbContextOptions<MatosKCDbContext>>();
                services.AddScoped(_ => new DbContextOptionsBuilder<MatosKCDbContext>().UseInMemoryDatabase(database, root).Options);
                services.AddScoped(provider => new MatosKCDbContext(provider.GetRequiredService<DbContextOptions<MatosKCDbContext>>(),
                    provider.GetRequiredService<AuditContext>(), provider.GetRequiredService<AgencyScope>()));
                services.RemoveAll<IObjectStorage>(); services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
                services.RemoveAll<IUnitOfWork>(); services.AddScoped<IUnitOfWork, UnitOfWork>();
            });
        }
        public async Task<HttpClient> Login(string email)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            if (!seeded)
            {
                using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
                db.AddRange(new Agency(Agency83, "83", 83), new Agency(Agency33, "33", 33));
                var hash = new IdentityPasswordHasher().HashPassword("Test123!");
                db.AddRange(new Account("83", "admin83@example.test", hash, true, Role.Admin, Agency83),
                    new Account("33", "admin33@example.test", hash, true, Role.Admin, Agency33),
                    new Account("Super", "super@example.test", hash, true, Role.SuperAdmin, null));
                await db.SaveChangesAsync(); seeded = true;
            }
            await MatosKCApiFactory.RefreshCsrfAsync(client);
            await Json(await client.PostAsJsonAsync("/auth/login", new { email, password = "Test123!" }));
            await MatosKCApiFactory.RefreshCsrfAsync(client);
            return client;
        }
    }
    private sealed class UnitOfWork(MatosKCDbContext db) : IUnitOfWork
    {
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        { var result = await operation(ct); await db.SaveChangesAsync(ct); return result; }
    }
}
