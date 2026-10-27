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
using SkiaSharp;

public sealed class InspectionReportHtmlTests
{
    [Fact]
    public async Task ChangedTemplateKeepsHistoricalEvidenceAndRendersEachNewFindingExactlyOnce()
    {
        var agency = new Agency("Epinal", 83);
        var category = new EquipmentCategory("Camion", null);
        var equipment = new Equipment("Camion", category.Id, "DEMO-001");
        var client = new Client(Guid.NewGuid(), "Client <test>", "test@example.test", "0612345678", new CommandNumber(83, "001234"));
        var now = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var rental = new Rental(Guid.NewGuid(), client.Id, equipment.Id, now, agency.Id);
        var templateId = Guid.NewGuid();
        var template = new InspectionTemplate(templateId,
            [new InspectionTemplatePoint(Guid.NewGuid(), templateId, "Nouveau point", "Contrôle", null)], category.Id);
        var inspection = new Inspection(Guid.NewGuid(), rental.Id, templateId, InspectionType.Return,
            Guid.NewGuid(), "Luc", now);
        using var bitmap = new SKBitmap(16, 16);
        bitmap.Erase(SKColors.White);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        var historicalPhoto = Guid.NewGuid();
        var defect = Guid.NewGuid();
        inspection.PresentDefect(defect, "Porte <ancienne>", "Rayure & trace", now.AddDays(-1), [historicalPhoto]);
        inspection.ReviewDefect(defect, DefectReviewResult.Unchanged, null);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, null);
        foreach (var observation in new[] { "Impact & bosse", "Fissure <visible>" })
        {
            var finding = Guid.NewGuid();
            inspection.SetFinding(finding, template.Points[0].Id, observation);
            inspection.AddEvidencePhoto(finding, Guid.NewGuid());
        }
        inspection.Sign("Client", bytes, now);
        inspection.Validate(template, now);
        var generator = new InspectionReportGenerator(
            Port<IClientRepository>.Create(_ => Task.FromResult<Client?>(client)),
            Port<IEquipmentRepository>.Create(_ => Task.FromResult<Equipment?>(equipment)),
            Port<IAgencyRepository>.Create(_ => Task.FromResult<Agency?>(agency)),
            Port<IInspectionMediaRepository>.Create(args => Task.FromResult<InspectionMedia?>(new InspectionMedia(
                (Guid)args![0]!, inspection.Id, Guid.NewGuid(), "photo", "photo.png", "image/png", bytes.Length))),
            Port<IObjectStorage>.Create(_ => Task.FromResult<StoredObject?>(new StoredObject(new MemoryStream(bytes), "image/png", bytes.Length))));

        var html = await generator.BuildHtmlAsync(inspection, rental, template, TestContext.Current.CancellationToken);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(html, "<h3>Nouveau point</h3>").Count);
        Assert.Contains("Porte &lt;ancienne&gt;", html);
        Assert.Contains("Rayure &amp; trace", html);
        Assert.Contains("Impact &amp; bosse", html);
        Assert.Contains("Fissure &lt;visible&gt;", html);
        Assert.Contains("01/01/2026 à 10:00", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.DoesNotContain("{{", html);
        Assert.DoesNotContain(rental.Id.ToString(), html);
        Assert.DoesNotContain(inspection.Id.ToString(), html);
        Assert.DoesNotContain(template.Id.ToString(), html);
        Assert.Equal(4, System.Text.RegularExpressions.Regex.Matches(html, "<img ").Count);
    }

    public class Port<T> : DispatchProxy where T : class
    {
        private Func<object?[]?, object?> handler = null!;
        public static T Create(Func<object?[]?, object?> handler)
        {
            var proxy = DispatchProxy.Create<T, Port<T>>();
            ((Port<T>)(object)proxy).handler = handler;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => handler(args);
    }
}
