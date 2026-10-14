namespace MatosKC.Infrastructure.Tests.Persistence;

using System.Reflection;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Equipments;
using MatosKC.Infrastructure.Reports;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class InspectionReportTests
{
    [Fact]
    public async Task Report_ContainsImagesAndSignatureAndPaginatesLongObservations()
    {
        var category = new EquipmentCategory("Camion", null);
        var item = new Equipment("Camion de location", category.Id, "AB-123-CD");
        var agency = new Agency("Epinal", 83);
        var client = new Client(Guid.NewGuid(), "Client Test", "client@example.test", "0612345678", new CommandNumber(83, "001234"));
        var now = DateTimeOffset.UtcNow;
        var rental = new Rental(Guid.NewGuid(), client.Id, item.Id, now, agency.Id);
        Guid templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Etat extérieur", null)], category.Id);
        var inspection = new InspectionEntity(Guid.NewGuid(), rental.Id, templateId,
            InspectionType.Departure, Guid.NewGuid(), "Maxime", now);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant,
            string.Join(" ", Enumerable.Repeat("Observation détaillée du matériel et des défauts constatés.", 100)));
        Guid photoId = Guid.NewGuid();
        inspection.AddAnswerPhoto(inspection.Answers[0].Id, photoId);
        using var bitmap = new SKBitmap(160, 60);
        bitmap.Erase(SKColors.White);
        using var canvas = new SKCanvas(bitmap);
        using var pen = new SKPaint { Color = SKColors.Black, StrokeWidth = 3 };
        canvas.DrawLine(10, 40, 130, 10, pen);
        using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        byte[] png = image.ToArray();
        inspection.Sign("Client Test", png, now);
        inspection.Validate(template, now);
        var photo = new InspectionMedia(photoId, inspection.Id, inspection.Answers[0].Id,
            "photo", "photo.png", "image/png", png.Length);
        var generator = new InspectionReportGenerator(
            Port<IClientRepository>.With("GetByIdAsync", Task.FromResult<Client?>(client)),
            Port<IEquipmentRepository>.With("RetrieveByIdAsync", Task.FromResult<Equipment?>(item)),
            Port<IAgencyRepository>.With("GetByIdAsync", Task.FromResult<Agency?>(agency)),
            Port<IInspectionMediaRepository>.With("GetByIdAsync", Task.FromResult<InspectionMedia?>(photo)),
            Port<IObjectStorage>.With("RetrieveAsync", Task.FromResult<StoredObject?>(
                new StoredObject(new MemoryStream(png), "image/png", png.Length))));
        byte[] pdf = await generator.GenerateAsync(inspection, rental, template, TestContext.Current.CancellationToken);
        using var input = new MemoryStream(pdf);
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount > 1);
        Assert.Equal("MatosKC - Etat des lieux", document.Info.Title);
        Assert.True(pdf.Length > png.Length);

        if (Environment.GetEnvironmentVariable("MATOSKC_REPORT_SAMPLE") is { } path)
        {
            await File.WriteAllBytesAsync(path, pdf, TestContext.Current.CancellationToken);
        }
    }

    public class Port<T> : DispatchProxy where T : class
    {
        private string _method = "";
        private object? _value;

        public static T With(string method, object value)
        {
            T proxy = Create<T, Port<T>>();
            ((Port<T>)(object)proxy)._method = method;
            ((Port<T>)(object)proxy)._value = value;

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == _method ? _value : throw new NotSupportedException(targetMethod?.Name);
    }
}
