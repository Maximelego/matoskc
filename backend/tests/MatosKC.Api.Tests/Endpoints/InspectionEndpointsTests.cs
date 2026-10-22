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
    private const string SignatureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAKAAAAA8CAYAAADha7EVAAACTUlEQVR4nO2c227DIBAFS9X//2X3yZKFYoK5nbN45q1NlcDusAsJaTqO4/gBEPGrHgC8GwQEKQgIUhAQpCAgSEFAkIKAIAUBQQoCBiSl9JNSUg9jCH/qAUA9uXTnz5E/zKICBqFU8SJXQyqgOXdynVXvfDxqNUxcRvCkVNU+pSz/+yhptW7B+ep+C6WqdydW/vsoBxXbCvgpeKZDHca3dtv6PM5xsxTwafuJzqz5RhDR6hASoWWMZlTVKz2H80HFpgKWEhFhJT9lpni1r+cQRwsBvwXHNXgtqLcXbotZLmCtXDtIuLrqlXARUSrgU6miSugkXo5aRImAPQlRB+wJ6nZbi3JhLxdwRDWIIKFz1btDEdelAo5aac6tOKJ4OStFXCbgaGncJIzSbp+wQsQlAs6SxUXCHareHbNjPFXAFYlR7gd3Fi9nVpynCbgyOasl3LHd1jI61lMEXN0aV77em6peiVEiDr8PqNiX1X5y0sPd/brSHb2dGTXnoQIqDwWzJCxd7HyjeFdGLL4h17FcEvTp5kwPLvNyp+vttN49oGOSevcnjnPala4K6PI+3DdSSk2fM19xnNcONO8BneVr2Q+2fBEI+mlqwc7yXakZJ+1Wy6MWHC1ZpUMJ7daD6goYTb4rtSfjCHPZjaoKGKXltrLTXKJRdQjJExQtYaXxRpvLblSfgs9ERU1YPn5Otx7IvxUH78b6nxPB/iAgSEFAkIKAIAUBQQoCghQEBCkICFIQEKQgIEhBQJCCgCAFAUEKAoIUBAQpCAhSEBCk/AOnlJcgDPWtMwAAAABJRU5ErkJggg==";

    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAACgAAAAeCAIAAADRv8uKAAAALklEQVR4nO3NMQEAMAgAoLlIJjG2sazg5wMFiM56F/7JKhaLxWKxWCwWi8XilQH93wGGpwUmBgAAAABJRU5ErkJggg=="
    );

    private async Task<(Guid EquipmentId, Guid CategoryId, int AgencyCode)> SeedAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatosKCDbContext>();
        var category = new MatosKC.Domain.Equipments.EquipmentCategory("Test " + Guid.NewGuid(), null);
        var equipment = new MatosKC.Domain.Equipments.Equipment("Camion", category.Id, Guid.NewGuid().ToString());
        var agency = await db.Agencies.SingleAsync(value => value.Code == 83);
        db.AddRange(category, equipment);
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
            points = new[] { new { description = "Etat extérieur", descriptionDetail = "Le point sera à préciser", photoId = (Guid?)null } }
        }, cancellationToken: TestContext.Current.CancellationToken);

    private static Task<HttpResponseMessage> DepartureAsync(HttpClient client, Guid equipmentId, int agencyCode) =>
        client.PostAsJsonAsync("/inspections/departure", new
        {
            equipmentId,
            operatorFirstName = "Maxime",
            client = new { displayName = "Client", email = "client@example.test", phone = "0612345678", agencyCode, commandValue = "001234" }
        }, cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<HttpResponseMessage> SaveAsync(HttpClient client, Guid inspectionId, Guid pointId, bool defective)
    {
        var response = await client.PutAsJsonAsync($"/inspections/{inspectionId}/draft", new
        {
            operatorFirstName = "Maxime",
            answers = new[] { new { templatePointId = pointId, result = defective ? "NonCompliant" : "Compliant", observation = defective ? "Défaut" : null } }
        }, cancellationToken: TestContext.Current.CancellationToken);

        if (response.IsSuccessStatusCode)
        {
            await SignAsync(client, inspectionId);
        }

        return response;
    }

    private static async Task SignAsync(HttpClient client, Guid id)
    {
        using var response = await client.PutAsJsonAsync($"/inspections/{id}/signature", new
        {
            signerName = "Client", accepted = true,
            signaturePngBase64 = SignatureBase64
        }, cancellationToken: TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DefectResolution_PreservesSignedReportAndSharesOnlyEquipmentPhotos()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        var template = await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var departure = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = departure.GetProperty("id").GetGuid();
        Guid pointId = template.GetProperty("points")[0].GetProperty("id").GetGuid();
        var saved = await JsonAsync(await SaveAsync(client, id, pointId, true));
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent(Png);
        image.Headers.ContentType = new("image/png");
        form.Add(image, "file", "defect.png");
        Guid answerId = saved.GetProperty("answers")[0].GetProperty("id").GetGuid();
        var photo = await JsonAsync(await client.PostAsync($"/inspections/{id}/answers/{answerId}/photos", form, TestContext.Current.CancellationToken));
        Guid photoId = photo.GetProperty("id").GetGuid();
        await SignAsync(client, id);
        await JsonAsync(await client.PostAsync($"/inspections/{id}/validation", null, TestContext.Current.CancellationToken));
        await JsonAsync(await client.PostAsync($"/inspections/{id}/validation", null, TestContext.Current.CancellationToken));
        byte[] report = await client.GetByteArrayAsync($"/inspections/{id}/report", TestContext.Current.CancellationToken);

        using var otherAgency = await factory.CreateAuthenticatedClientAsync(Role.Agency);
        Assert.Equal(HttpStatusCode.NotFound, (await otherAgency.GetAsync($"/equipments/{seed.EquipmentId}/defects", TestContext.Current.CancellationToken)).StatusCode);
        var defects = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}/defects?status=Open", TestContext.Current.CancellationToken));
        Assert.Equal(1, defects.GetProperty("total").GetInt32());
        Guid defectId = defects.GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(Png, await client.GetByteArrayAsync(
            $"/equipments/{seed.EquipmentId}/defects/{defectId}/photos/{photoId}", TestContext.Current.CancellationToken));
        using var privateInspection = await otherAgency.GetAsync($"/inspections/{id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, privateInspection.StatusCode);
        using var foreignPhoto = await otherAgency.GetAsync(
            $"/equipments/{seed.EquipmentId}/defects/{defectId}/photos/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, foreignPhoto.StatusCode);

        await JsonAsync(await client.PostAsJsonAsync($"/equipments/{seed.EquipmentId}/maintenances", new
        {
            id = Guid.NewGuid(), performedAt = DateTimeOffset.UtcNow, performedBy = "Atelier",
            workDescription = "Tuyau remplacé", resolvedDefectIds = new[] { defectId }
        }, TestContext.Current.CancellationToken));
        var open = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}/defects?status=Open", TestContext.Current.CancellationToken));
        Assert.Equal(0, open.GetProperty("total").GetInt32());
        var history = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}/defects?status=Resolved", TestContext.Current.CancellationToken));
        Assert.Equal(1, history.GetProperty("total").GetInt32());
        Assert.Equal(report, await client.GetByteArrayAsync($"/inspections/{id}/report", TestContext.Current.CancellationToken));
        var equipment = await JsonAsync(await client.GetAsync($"/equipments/{seed.EquipmentId}", TestContext.Current.CancellationToken));
        Assert.Equal("Borrowed", equipment.GetProperty("status").GetString());
    }

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
        var persisted = await JsonAsync(await client.GetAsync($"/inspections/{returnId}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(persisted.GetProperty("validatedAt").GetDateTimeOffset(), retry.GetProperty("validatedAt").GetDateTimeOffset());
        Assert.InRange((validatedReturn.GetProperty("validatedAt").GetDateTimeOffset()
            - persisted.GetProperty("validatedAt").GetDateTimeOffset()).Duration(), TimeSpan.Zero, TimeSpan.FromMicroseconds(1));
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
            await JsonAsync(await client.PatchAsJsonAsync($"/equipments/{seed.EquipmentId}/status", new { status = "Available", reason = "Défaut examiné : remise en location autorisée." }, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(HttpStatusCode.Created,
                (await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode)).StatusCode);
        }
    }

    [Fact]
    public async Task Signature_IsRequiredAndFinalReportRemainsIdenticalAfterRetry()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        var template = await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var inspection = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = inspection.GetProperty("id").GetGuid();
        Guid pointId = template.GetProperty("points")[0].GetProperty("id").GetGuid();
        using var draft = await client.PutAsJsonAsync($"/inspections/{id}/draft", new
        {
            operatorFirstName = "Maxime",
            answers = new[] { new { templatePointId = pointId, result = "Compliant", observation = (string?)null } }
        }, cancellationToken: TestContext.Current.CancellationToken);
        draft.EnsureSuccessStatusCode();
        using var unsigned = await client.PostAsync($"/inspections/{id}/validation", null,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsigned.StatusCode);
        using var missingReport = await client.GetAsync($"/inspections/{id}/report", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, missingReport.StatusCode);
        await SignAsync(client, id);
        var validated = await JsonAsync(await client.PostAsync($"/inspections/{id}/validation", null,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.True(validated.GetProperty("hasReport").GetBoolean());
        var report = await client.GetByteArrayAsync($"/inspections/{id}/report", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(report, 0, 4));
        await JsonAsync(await client.PostAsync($"/inspections/{id}/validation", null,
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(report, await client.GetByteArrayAsync($"/inspections/{id}/report", cancellationToken: TestContext.Current.CancellationToken));
        using var mutation = await client.PutAsJsonAsync($"/inspections/{id}/signature", new
        {
            signerName = "Autre", accepted = true, signaturePngBase64 = SignatureBase64
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, mutation.StatusCode);
    }

    [Fact]
    public async Task InvalidPhoto_IsRejectedAtUploadWithoutInvalidatingSignatureOrSavingMetadata()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        var template = await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var inspection = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = inspection.GetProperty("id").GetGuid();
        var saved = await JsonAsync(await SaveAsync(client, id,
            template.GetProperty("points")[0].GetProperty("id").GetGuid(), true));
        Guid answerId = saved.GetProperty("answers")[0].GetProperty("id").GetGuid();
        using var form = new MultipartFormDataContent();
        var invalid = new ByteArrayContent([1, 2, 3]);
        invalid.Headers.ContentType = new("image/png");
        form.Add(invalid, "file", "broken.png");
        using var response = await client.PostAsync($"/inspections/{id}/answers/{answerId}/photos", form,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var persisted = await JsonAsync(await client.GetAsync($"/inspections/{id}", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Draft", persisted.GetProperty("status").GetString());
        Assert.Equal(0, persisted.GetProperty("answers")[0].GetProperty("photoIds").GetArrayLength());
        Assert.NotEqual(JsonValueKind.Null, persisted.GetProperty("signedAt").ValueKind);
    }

    [Fact]
    public async Task Templates_RequireAdministratorAndExistingCategoryAndPreserveOrder()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var agency = await factory.CreateAuthenticatedClientAsync(Role.Agency);
        var seed = await SeedAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await TemplateAsync(agency, seed.CategoryId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TemplateAsync(admin, Guid.NewGuid())).StatusCode);
        using var response = await admin.PostAsJsonAsync("/inspection-templates", new
        {
            equipmentCategoryId = seed.CategoryId,
            points = new[] { new { description = "Premier", descriptionDetail = "Le point sera à préciser", photoId = (Guid?)null },
                new { description = "Second", descriptionDetail = "Le point sera à préciser", photoId = (Guid?)null } }
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var template = await JsonAsync(response);
        Guid id = template.GetProperty("id").GetGuid();
        var latest = await JsonAsync(await admin.GetAsync($"/equipment-categories/{seed.CategoryId}/inspection-template",
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(id, latest.GetProperty("id").GetGuid());
        Assert.Equal("Premier", latest.GetProperty("points")[0].GetProperty("description").GetString());
        Assert.Equal("Second", latest.GetProperty("points")[1].GetProperty("description").GetString());
        using var empty = await admin.PostAsJsonAsync("/inspection-templates", new
        {
            equipmentCategoryId = seed.CategoryId, points = Array.Empty<object>()
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
    }

    [Fact]
    public async Task SearchAndCancellation_KeepAuditAndAllowAnotherDeparture()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var inspection = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = inspection.GetProperty("id").GetGuid();
        var draftPage = await JsonAsync(await client.GetAsync($"/inspections?equipmentId={seed.EquipmentId}&status=Draft&limit=1",
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, draftPage.GetProperty("total").GetInt32());
        Assert.Equal(id, draftPage.GetProperty("inspections")[0].GetProperty("id").GetGuid());
        using var cancelled = await client.PostAsJsonAsync($"/inspections/{id}/cancellation", new { reason = "Abandon" },
            cancellationToken: TestContext.Current.CancellationToken);
        var result = await JsonAsync(cancelled);
        Assert.Equal("Cancelled", result.GetProperty("status").GetString());
        Assert.Equal("Abandon", result.GetProperty("cancellationReason").GetString());
        var rental = await JsonAsync(await client.GetAsync($"/rentals/{inspection.GetProperty("rentalId").GetGuid()}",
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("Cancelled", rental.GetProperty("status").GetString());
        var page = await JsonAsync(await client.GetAsync($"/inspections?equipmentId={seed.EquipmentId}&commandNumber=001234",
            cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal("Cancelled", page.GetProperty("inspections")[0].GetProperty("status").GetString());
        using var invalid = await client.GetAsync("/inspections?commandNumber=123", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode)).StatusCode);
    }

    [Fact]
    public async Task Cancellation_RequiresCsrfAndOperatorRole()
    {
        using var client = await factory.CreateAuthenticatedClientAsync();
        var seed = await SeedAsync();
        await JsonAsync(await TemplateAsync(client, seed.CategoryId));
        var inspection = await JsonAsync(await DepartureAsync(client, seed.EquipmentId, seed.AgencyCode));
        Guid id = inspection.GetProperty("id").GetGuid();
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var forbidden = await admin.PostAsJsonAsync($"/inspections/{id}/cancellation", new { reason = "Abandon" },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using var rejected = await client.PostAsJsonAsync($"/inspections/{id}/cancellation", new { reason = "Abandon" },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    [Fact]
    public async Task TemplateIllustration_UploadReferenceDownloadAndPermissions()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(Role.Admin);
        using var agency = await factory.CreateAuthenticatedClientAsync(Role.Agency, 83);
        using var anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var seed = await SeedAsync();
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new("image/png");
        form.Add(file, "file", "guide.png");
        using var upload = await admin.PostAsync("/inspection-templates/photos", form, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var photo = await JsonAsync(upload);
        Guid id = photo.GetProperty("id").GetGuid();
        Assert.Equal(Png, await agency.GetByteArrayAsync($"/inspection-templates/photos/{id}", cancellationToken: TestContext.Current.CancellationToken));
        using var deniedRead = await anonymous.GetAsync($"/inspection-templates/photos/{id}", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, deniedRead.StatusCode);
        using var forbiddenForm = new MultipartFormDataContent();
        var forbiddenFile = new ByteArrayContent(Png);
        forbiddenFile.Headers.ContentType = new("image/png");
        forbiddenForm.Add(forbiddenFile, "file", "guide.png");
        using var deniedWrite = await agency.PostAsync("/inspection-templates/photos", forbiddenForm,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
        foreach (int version in new[] { 1, 2 })
        {
            var created = await JsonAsync(await admin.PostAsJsonAsync("/inspection-templates", new
            {
                equipmentCategoryId = seed.CategoryId,
                points = new[] { new { description = "Contrôle " + version, descriptionDetail = "Consigne", photoId = id } }
            }, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(id, created.GetProperty("points")[0].GetProperty("photoId").GetGuid());
        }
        Assert.Equal(Png, await agency.GetByteArrayAsync($"/inspection-templates/photos/{id}", cancellationToken: TestContext.Current.CancellationToken));
        using var unknown = await admin.PostAsJsonAsync("/inspection-templates", new
        {
            equipmentCategoryId = seed.CategoryId,
            points = new[] { new { description = "Contrôle", descriptionDetail = "Consigne", photoId = Guid.NewGuid() } }
        }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        using var invalidForm = new MultipartFormDataContent();
        var invalidFile = new ByteArrayContent([1, 2, 3]);
        invalidFile.Headers.ContentType = new("image/png");
        invalidForm.Add(invalidFile, "file", "bad.png");
        using var invalid = await admin.PostAsync("/inspection-templates/photos", invalidForm,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
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
        var ownPage = await JsonAsync(await agency.GetAsync("/inspections", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(0, ownPage.GetProperty("total").GetInt32());
        Assert.Equal(0, ownPage.GetProperty("inspections").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound,
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
