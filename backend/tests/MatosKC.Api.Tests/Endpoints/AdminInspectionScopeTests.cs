namespace MatosKC.Api.Tests.Endpoints;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Persistence;
using MatosKC.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection(ApiTestCollection.Name)]
public sealed class AdminInspectionScopeTests(MatosKCApiFactory factory)
{
    [Fact]
    public async Task AdminScopeIsEnforcedForSearchDirectAccessAndPrivateMedia_WhileSuperAdminRemainsGlobal()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var foreignAdmin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var superAdmin = await factory.CreateAuthenticatedClientAsync(Role.SuperAdmin);
        var ownAgencyId = (await Json(await admin.GetAsync("/auth/me", ct))).GetProperty("agencyId").GetGuid();
        var otherAgencyId = (await Json(await foreignAdmin.GetAsync("/auth/me", ct))).GetProperty("agencyId").GetGuid();
        Guid ownEquipmentId; Guid otherEquipmentId; Guid categoryId; int ownCode; int otherCode;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
            var category = new EquipmentCategory("Scoped " + Guid.NewGuid(), null);
            var first = new Equipment("Own", category.Id, Guid.NewGuid().ToString());
            var second = new Equipment("Other", category.Id, Guid.NewGuid().ToString());
            db.AddRange(category, first, second);
            await db.SaveChangesAsync(ct);
            categoryId = category.Id; ownEquipmentId = first.Id; otherEquipmentId = second.Id;
            ownCode = (await db.Agencies.SingleAsync(value => value.Id == ownAgencyId, ct)).Code;
            otherCode = (await db.Agencies.SingleAsync(value => value.Id == otherAgencyId, ct)).Code;
        }
        await Json(await superAdmin.PostAsJsonAsync("/inspection-templates", new
        {
            equipmentCategoryId = categoryId,
            points = new[] { new { description = "Etat", descriptionDetail = "Contrôle", photoId = (Guid?)null } }
        }, ct));
        async Task<JsonElement> Departure(Guid equipmentId, int agencyCode) => await Json(
            await superAdmin.PostAsJsonAsync("/inspections/departure", new
            {
                equipmentId, operatorFirstName = "Maxime",
                client = new { displayName = "Client", email = "client@example.test", phone = "0612345678", agencyCode, commandValue = "001234" }
            }, ct));
        var own = await Departure(ownEquipmentId, ownCode);
        var other = await Departure(otherEquipmentId, otherCode);
        var ownId = own.GetProperty("id").GetGuid();
        var otherId = other.GetProperty("id").GetGuid();
        var otherRentalId = other.GetProperty("rentalId").GetGuid();
        var page = await Json(await admin.GetAsync("/inspections?limit=1", ct));
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal(ownId, page.GetProperty("inspections")[0].GetProperty("id").GetGuid());
        var emptyPage = await Json(await admin.GetAsync("/inspections?offset=1&limit=1", ct));
        Assert.Equal(1, emptyPage.GetProperty("total").GetInt32());
        Assert.Empty(emptyPage.GetProperty("inspections").EnumerateArray());
        var filtered = await Json(await admin.GetAsync($"/inspections?equipmentId={otherEquipmentId}", ct));
        Assert.Equal(0, filtered.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.GetAsync($"/inspections?agencyId={otherAgencyId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.GetAsync($"/inspections?rentalId={otherRentalId}", ct)).StatusCode);
        foreach (var path in new[]
        {
            $"/inspections/{otherId}", $"/rentals/{otherRentalId}",
            $"/rentals/{otherRentalId}/departure-inspection", $"/equipments/{otherEquipmentId}/current-rental",
            $"/inspections/{otherId}/report", $"/inspections/{otherId}/signature",
            $"/inspections/{otherId}/photos/{Guid.NewGuid()}"
        })
        {
            using var response = await admin.GetAsync(path, ct);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/inspections/{ownId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await superAdmin.GetAsync($"/inspections/{otherId}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await superAdmin.GetAsync($"/rentals/{otherRentalId}", ct)).StatusCode);
        var global = await Json(await superAdmin.GetAsync($"/inspections?agencyId={otherAgencyId}", ct));
        Assert.Equal(otherId, global.GetProperty("inspections")[0].GetProperty("id").GetGuid());
        using (var scope = factory.Services.CreateScope())
        {
            var repository = new InspectionsRepository(scope.ServiceProvider.GetRequiredService<MatosKCDbContext>());
            Assert.Equal(ownId, Assert.Single(await repository.ListAsync(null, null, null, 0, 1, ownAgencyId, ct)).Id);
            Assert.Empty(await repository.ListAsync(null, null, null, 1, 1, ownAgencyId, ct));
        }
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using (response)
        {
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.Clone();
        }
    }
}
