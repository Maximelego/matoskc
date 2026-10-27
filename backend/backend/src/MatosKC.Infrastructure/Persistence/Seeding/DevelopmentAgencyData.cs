namespace MatosKC.Infrastructure.Persistence.Seeding;

using System.Security.Cryptography;
using System.Text;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Maintenance;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public static class DevelopmentAgencyData
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    private static readonly byte[] SignaturePng = CreateDemoSignature();

    private static byte[] CreateDemoSignature()
    {
        using var bitmap = new SKBitmap(160, 60);
        bitmap.Erase(SKColors.White);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 28);
        canvas.DrawText("DEMO", 16, 40, font, paint);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        new MatosKC.Infrastructure.Reports.InspectionSignatureValidator(
            new MatosKC.Infrastructure.Storage.ImageContentValidator()).Validate(bytes);
        return bytes;
    }
    public static Guid Id(int code, string kind, string value) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"MatosKC/demo/{code}/{kind}/{value}"))[..16]);

    public static Guid Map(Guid id, int code) => code == 83 ? id : Id(code, "map", id.ToString());

    public static (EquipmentCategory[] Categories, Equipment[] Equipments, InspectionTemplate[] Templates) Create(Agency agency)
    {
        var categories = DevelopmentData.CreateCategories().Select(source =>
        {
            var category = new EquipmentCategory(Map(source.Id, agency.Code), source.Name, source.Description);
            category.AssignAgency(agency.Id);
            return category;
        }).ToArray();
        var equipments = DevelopmentData.CreateEquipments().Select(source =>
        {
            var equipment = new Equipment(Map(source.Id, agency.Code), source.Name, Map(source.CategoryId, agency.Code),
                agency.Code == 83 ? source.SerialNumber : $"{source.SerialNumber}-{agency.Code}");
            equipment.AssignAgency(agency.Id);
            ApplyStatus(equipment, source.Status);
            return equipment;
        }).ToArray();
        var templates = DevelopmentInspectionTemplates.Create().Select(source =>
        {
            var id = Map(source.Id, agency.Code);
            var template = new InspectionTemplate(id, source.Points.Select(point => new InspectionTemplatePoint(
                Map(point.Id, agency.Code), id, point.Description, point.DescriptionDetail, null, point.Position)),
                Map(source.EquipmentCategoryId, agency.Code));
            template.AssignAgency(agency.Id);
            return template;
        }).ToArray();
        return (categories, equipments, templates);
    }

    public static async Task SeedAsync(MatosKCDbContext db, IObjectStorage storage,
        IInspectionReportGenerator reports, CancellationToken ct)
    {
        await storage.EnsureReadyAsync(ct);
        foreach (var agency in await db.Agencies.Where(value => new[] { 83, 33, 12 }.Contains(value.Code)).ToArrayAsync(ct))
        {
            // Equipment status and its rental/inspection history must become visible together.
            await using var transaction = db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(ct) : null;
            try
            {
                var data = Create(agency);
                foreach (var category in data.Categories)
                    if (!await db.EquipmentCategories.AnyAsync(value => value.Id == category.Id, ct)) db.Add(category);
                await db.SaveChangesAsync(ct);
                foreach (var equipment in data.Equipments)
                    if (!await db.Equipments.AnyAsync(value => value.Id == equipment.Id, ct)) db.Add(equipment);
                await db.SaveChangesAsync(ct);
                foreach (var template in data.Templates)
                    if (!await db.InspectionTemplates.AnyAsync(value => value.EquipmentCategoryId == template.EquipmentCategoryId, ct)) db.Add(template);
                await db.SaveChangesAsync(ct);
                foreach (var source in DevelopmentData.CreateEquipmentPhotos())
                {
                    var id = Map(source.Id, agency.Code);
                    if (await db.EquipmentPhotos.AnyAsync(value => value.Id == id, ct)) continue;
                    var equipmentId = Map(source.EquipmentId, agency.Code);
                    var photo = new EquipmentPhoto(id, equipmentId, $"equipments/{equipmentId}/photos/{id}",
                        source.OriginalFileName, "image/png", Png.Length, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
                    await storage.StoreAsync(photo.ObjectKey, new MemoryStream(Png), "image/png", ct);
                    db.Add(photo);
                }
                await db.SaveChangesAsync(ct);
                var account = await db.Accounts.FirstAsync(value => value.AgencyId == agency.Id && value.Role == Role.Agency, ct);
                var admin = await db.Accounts.FirstAsync(value => value.AgencyId == agency.Id && value.Role == Role.Admin, ct);
                foreach (var sequence in new[] { 1, 2, 3, 4, 6, 9, 11 })
                {
                    var equipmentId = Map(Guid.Parse($"20000000-0000-0000-0000-{sequence:D12}"), agency.Code);
                    var equipment = await db.Equipments.SingleAsync(value => value.Id == equipmentId, ct);
                    // Never modify or supersede an existing operator's rental history.
                    if (await db.Rentals.AnyAsync(value => value.EquipmentId == equipmentId, ct)
                        || await db.EquipmentMaintenances.AnyAsync(value => value.EquipmentId == equipmentId, ct)) continue;
                    var expected = data.Equipments.Single(value => value.Id == equipmentId);
                    if (equipment.Status != expected.Status) continue;
                    var template = await db.InspectionTemplates.Include(value => value.Points)
                        .Where(value => value.EquipmentCategoryId == equipment.CategoryId)
                        .OrderByDescending(value => value.CreatedAt).FirstAsync(ct);
                    if (template.Points.Count == 0) continue;
                    var start = new DateTimeOffset(2026, 10, 1, 8, 0, 0, TimeSpan.Zero).AddDays(sequence);
                    var client = new Client(Id(agency.Code, "client", sequence.ToString()), $"Client démo {agency.Code}/{sequence}",
                        "demo@example.test", "0612345678", new CommandNumber(agency.Code, sequence.ToString("D6")));
                    var rental = new Rental(Id(agency.Code, "rental", sequence.ToString()), client.Id, equipmentId, start, agency.Id);
                    var departure = new InspectionEntity(Id(agency.Code, "departure", sequence.ToString()), rental.Id, template.Id,
                        InspectionType.Departure, account.Id, $"Opérateur {agency.Code}", start);
                    db.AddRange(client, rental, departure);
                    if (sequence == 1) { await db.SaveChangesAsync(ct); continue; }
                    foreach (var point in template.Points) departure.SetAnswer(point.Id, InspectionResult.Compliant, null);
                    EquipmentDefect? defect = null;
                    if (sequence is 3 or 6 or 9)
                    {
                        var point = template.Points.OrderBy(value => value.Position).First();
                        departure.SetAnswer(point.Id, InspectionResult.NonCompliant, "Rayure constatée pendant le contrôle de démonstration.");
                        var answer = departure.Answers.Single(value => value.TemplatePointId == point.Id);
                        var photoId = Id(agency.Code, "defect-photo", sequence.ToString());
                        var key = $"inspection-photos/{departure.Id}/{photoId}";
                        departure.AddAnswerPhoto(answer.Id, photoId);
                        await storage.StoreAsync(key, new MemoryStream(Png), "image/png", ct);
                        db.Add(new InspectionMedia(photoId, departure.Id, answer.Id, key, "defaut-demo.png", "image/png", Png.Length));
                        defect = new EquipmentDefect(Id(agency.Code, "defect", sequence.ToString()), equipmentId, departure.Id,
                            answer.Id, point.Description, answer.Comment!, start, account.Id, departure.OperatorFirstName);
                        db.Add(defect);
                        db.Add(new DefectOccurrence(Id(agency.Code, "occurrence", sequence.ToString()), defect.Id,
                            departure.Id, answer.Id, point.Description, answer.Comment!, start, [photoId], account.Id, departure.OperatorFirstName));
                    }
                    departure.Sign("Client de démonstration", SignaturePng, start);
                    departure.Validate(template, start);
                    rental.ConfirmDeparture(departure, start);
                    if (sequence is 3 or 4 or 9)
                    {
                        var returnedAt = start.AddHours(4);
                        var inspection = new InspectionEntity(Id(agency.Code, "return", sequence.ToString()), rental.Id, template.Id,
                            InspectionType.Return, account.Id, $"Opérateur {agency.Code}", returnedAt);
                        foreach (var point in template.Points) inspection.SetAnswer(point.Id, InspectionResult.Compliant, null);
                        if (defect is not null)
                        {
                            var photoId = Id(agency.Code, "defect-photo", sequence.ToString());
                            inspection.PresentDefect(defect.Id, defect.Description, defect.Observation, start, [photoId]);
                            inspection.ReviewDefect(defect.Id, DefectReviewResult.Unchanged, null);
                        }
                        inspection.Sign("Client de démonstration", SignaturePng, returnedAt);
                        inspection.Validate(template, returnedAt);
                        rental.ConfirmReturn(inspection, returnedAt);
                        db.Add(inspection);
                        if (sequence == 3 && defect is not null)
                            db.Add(EquipmentMaintenance.Record(Id(agency.Code, "maintenance", sequence.ToString()), equipmentId,
                                returnedAt.AddDays(1), $"Atelier {agency.Code}", "Contrôle et remise en état de démonstration.",
                                admin.Id, returnedAt.AddDays(1), [defect]));
                    }
                    await db.SaveChangesAsync(ct);
                }

                // Preventive maintenance does not need a rental, but it does need an intervention record.
                foreach (var sequence in new[] { 8, 12 })
                {
                    var equipmentId = Map(Guid.Parse($"20000000-0000-0000-0000-{sequence:D12}"), agency.Code);
                    var equipment = await db.Equipments.SingleAsync(value => value.Id == equipmentId, ct);
                    if (equipment.Status != EquipmentStatus.Maintenance
                        || await db.EquipmentMaintenances.AnyAsync(value => value.EquipmentId == equipmentId, ct)) continue;
                    var performedAt = new DateTimeOffset(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
                    db.Add(EquipmentMaintenance.Record(Id(agency.Code, "maintenance", sequence.ToString()), equipmentId,
                        performedAt, $"Atelier {agency.Code}", "Maintenance préventive de démonstration, matériel non proposé à la location.",
                        admin.Id, performedAt, []));
                }
                await db.SaveChangesAsync(ct);

                // Backfill only deterministic demo reports. Never rewrite a signed report or user history.
                foreach (var sequence in new[] { 1, 2, 3, 4, 6, 9, 11 })
                {
                    var rentalId = Id(agency.Code, "rental", sequence.ToString());
                    var rental = await db.Rentals.SingleOrDefaultAsync(value => value.Id == rentalId, ct);
                    if (rental is null) continue;
                    var inspections = await db.Inspections.Include(value => value.Answers)
                        .Include(value => value.KnownDefects).Include(value => value.Findings)
                        .Where(value => value.RentalId == rentalId && value.Status == InspectionStatus.Validated)
                        .ToArrayAsync(ct);
                    foreach (var inspection in inspections.Where(value => !value.HasReport))
                    {
                        var template = await db.InspectionTemplates.Include(value => value.Points)
                            .SingleAsync(value => value.Id == inspection.TemplateId, ct);
                        inspection.AttachReport(await reports.GenerateAsync(inspection, rental, template, ct));
                    }
                }
                await db.SaveChangesAsync(ct);
                await EnsureRentalConsistencyAsync(db, data.Equipments.Select(value => value.Id).ToArray(), ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
            }
            catch
            {
                if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
                db.ChangeTracker.Clear();
                throw;
            }
        }
    }

    private static async Task EnsureRentalConsistencyAsync(MatosKCDbContext db, Guid[] equipmentIds, CancellationToken ct)
    {
        var equipments = await db.Equipments.Where(value => equipmentIds.Contains(value.Id)).ToArrayAsync(ct);
        var active = await db.Rentals.Where(value => equipmentIds.Contains(value.EquipmentId)
            && value.Status == RentalStatus.Active).ToArrayAsync(ct);
        foreach (var equipment in equipments)
        {
            var rentals = active.Where(value => value.EquipmentId == equipment.Id).ToArray();
            if ((equipment.Status == EquipmentStatus.Borrowed && rentals.Length != 1)
                || (equipment.Status != EquipmentStatus.Borrowed && rentals.Length != 0))
                throw new InvalidOperationException($"Inconsistent development equipment {equipment.Id}: active rental and status disagree. Existing operator history was not replaced.");
            foreach (var rental in rentals)
                if (!await db.Inspections.AnyAsync(value => value.Id == rental.DepartureInspectionId
                    && value.RentalId == rental.Id && value.Type == InspectionType.Departure
                    && value.Status == InspectionStatus.Validated && value.SignaturePng != null, ct))
                    throw new InvalidOperationException($"Active rental {rental.Id} has no signed, validated departure inspection.");
        }
    }

    private static void ApplyStatus(Equipment equipment, EquipmentStatus status)
    {
        if (status == EquipmentStatus.Available) return;
        if (status == EquipmentStatus.Decommissioned) { equipment.ChangeStatus(status); return; }
        equipment.ChangeStatus(EquipmentStatus.Borrowed);
        if (status == EquipmentStatus.Borrowed) return;
        equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
        if (status == EquipmentStatus.ToBeDecided) return;
        equipment.ChangeStatus(EquipmentStatus.Unavailable);
        if (status == EquipmentStatus.Unavailable) return;
        equipment.ChangeStatus(EquipmentStatus.Maintenance);
    }
}
