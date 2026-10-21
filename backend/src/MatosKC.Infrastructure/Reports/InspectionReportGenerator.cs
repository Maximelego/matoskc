namespace MatosKC.Infrastructure.Reports;

using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using Microsoft.Playwright;
using SkiaSharp;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public sealed class InspectionReportGenerator(
    IClientRepository clients,
    IEquipmentRepository equipment,
    IAgencyRepository agencies,
    IInspectionMediaRepository media,
    IObjectStorage storage
) : IInspectionReportGenerator
{
    // Keep browser memory bounded when several agencies validate at once.
    private static readonly SemaphoreSlim RenderGate = new(1, 1);
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    private static readonly string TemplateDirectory = Path.Combine(
        AppContext.BaseDirectory, "Reports", "Templates");

    public async Task<byte[]> GenerateAsync(InspectionEntity inspection, Rental rental,
        InspectionTemplate template, CancellationToken ct = default)
    {
        if (inspection.Status != InspectionStatus.Validated || inspection.SignaturePng is null)
            throw new InvalidOperationException("Only a signed, validated inspection can be rendered.");

        var client = await clients.GetByIdAsync(rental.ClientId, ct)
            ?? throw new InspectionOperationException(404, "Client not found.");
        var item = await equipment.RetrieveByIdAsync(rental.EquipmentId, ct)
            ?? throw new InspectionOperationException(404, "Equipment not found.");
        var agency = rental.AgencyId.HasValue
            ? await agencies.GetByIdAsync(rental.AgencyId.Value, ct) : null;
        var points = new StringBuilder();
        foreach (var point in template.Points.OrderBy(value => value.Position))
        {
            ct.ThrowIfCancellationRequested();
            var answer = inspection.Answers.Single(value => value.TemplatePointId == point.Id);
            points.Append(await CardAsync(point.Description, answer.Comment,
                answer.Result == InspectionResult.Compliant ? "Aucun nouveau défaut" : "Nouveau défaut signalé",
                answer.Result == InspectionResult.Compliant ? "success" : "danger", answer.PhotosIds, ct));
        }

        var known = new StringBuilder();
        foreach (var review in inspection.KnownDefects)
        {
            known.Append(await CardAsync(review.Description, review.HistoricalObservation,
                "Défaut préexistant", "warning", review.HistoricalPhotoIds, ct));
            var status = review.Result switch
            {
                DefectReviewResult.Unchanged => "Toujours présent, inchangé",
                DefectReviewResult.Worsened => "Aggravé",
                _ => "À vérifier par la maintenance"
            };
            known.Append(await CardAsync(review.Description, review.Observation, status,
                review.Result == DefectReviewResult.Worsened ? "danger" : "warning", review.PhotoIds, ct));
        }
        foreach (var finding in inspection.Findings)
        {
            var point = template.Points.Single(value => value.Id == finding.TemplatePointId);
            points.Append(await CardAsync(point.Description, finding.Observation, "Nouveau défaut", "danger", finding.PhotoIds, ct));
        }

        var values = new Dictionary<string, string>
        {
            ["ThemeCss"] = await ReadAsync("theme.css", ct),
            ["ReportCss"] = await ReadAsync("inspection-report.css", ct),
            ["Type"] = inspection.Type == InspectionType.Departure ? "Départ" : "Retour",
            ["CommandNumber"] = H(client.CommandNumber.Value),
            ["ClientName"] = H(client.DisplayName),
            ["ClientEmail"] = H(client.Email), ["ClientPhone"] = H(client.Phone),
            ["AgencyName"] = H(agency?.Name ?? "Historique"),
            ["AgencyCode"] = H(client.CommandNumber.AgencyCode.ToString(French)),
            ["Operator"] = H(inspection.OperatorFirstName),
            ["StartedAt"] = Date(inspection.StartedAt), ["ValidatedAt"] = Date(inspection.ValidatedAt),
            ["EquipmentName"] = H(item.Name), ["SerialNumber"] = H(item.SerialNumber),
            ["KnownDefects"] = known.Length > 0 ? known.ToString() : "<p class=empty>Aucun défaut préexistant enregistré au début de cet état des lieux.</p>",
            ["Points"] = points.ToString(), ["AcceptanceText"] = H(inspection.AcceptanceText),
            ["SignerName"] = H(inspection.SignerName), ["SignedAt"] = Date(inspection.SignedAt),
            ["Signature"] = Image(inspection.SignaturePng, "Signature", "signature-image")
        };
        string html = Fill(await ReadAsync("inspection-report.html", ct), values);
        await RenderGate.WaitAsync(ct);
        try
        {
            ct.ThrowIfCancellationRequested();
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Timeout = 30000 });
            await using var context = await browser.NewContextAsync(new()
            {
                JavaScriptEnabled = false, Locale = "fr-FR", TimezoneId = "Europe/Paris"
            });
            // All resources are embedded. No HTTP requests or local file access from the template.
            await context.RouteAsync("**/*", route => route.AbortAsync());
            var page = await context.NewPageAsync().WaitAsync(ct);
            page.SetDefaultTimeout(30000);
            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.Load, Timeout = 30000 }).WaitAsync(ct);
            return await page.PdfAsync(new()
            {
                Format = "A4", PrintBackground = true, PreferCSSPageSize = true,
                DisplayHeaderFooter = true, HeaderTemplate = "<span></span>",
                FooterTemplate = "<div style='width:100%;font-size:8px;text-align:center;color:#66717e'>MatosKC · <span class='pageNumber'></span> / <span class='totalPages'></span></div>",
                Margin = new() { Top = "14mm", Bottom = "18mm", Left = "15mm", Right = "15mm" }
            }).WaitAsync(TimeSpan.FromSeconds(30), ct);
        }
        finally { RenderGate.Release(); }
    }

    private async Task<string> CardAsync(string title, string? observation, string status,
        string statusClass, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var photos = new StringBuilder();
        foreach (Guid id in ids)
        {
            var photo = await media.GetByIdAsync(id, ct)
                ?? throw new InspectionOperationException(409, "An inspection photo is missing.");
            var content = await storage.RetrieveAsync(photo.ObjectKey, ct)
                ?? throw new InspectionOperationException(409, "An inspection photo is unavailable.");
            await using var source = content.Content;
            using var bytes = new MemoryStream();
            await source.CopyToAsync(bytes, ct);
            photos.Append(Image(bytes.ToArray(), title, "defect-photo"));
        }
        return Fill(await ReadAsync("point.html", ct), new Dictionary<string, string>
        {
            ["Title"] = H(title), ["Observation"] = H(observation),
            ["Status"] = H(status), ["StatusClass"] = statusClass, ["Photos"] = photos.ToString()
        });
    }

    private static string Image(byte[] bytes, string alt, string cssClass)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null || (long)codec.Info.Width * codec.Info.Height > 25_000_000)
            throw new InspectionOperationException(422, "A report image is invalid or too large.");
        using var bitmap = SKBitmap.Decode(bytes);
        if (bitmap is null) throw new InspectionOperationException(422, "A report image cannot be decoded.");
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return $"<img class=\"{cssClass}\" alt=\"{H(alt)}\" src=\"data:image/png;base64,{Convert.ToBase64String(png.ToArray())}\">";
    }

    private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");
    private static string Date(DateTimeOffset? value) => value.HasValue
        ? TimeZoneInfo.ConvertTime(value.Value, Paris).ToString("dd/MM/yyyy 'à' HH:mm", French) : "";
    private static Task<string> ReadAsync(string name, CancellationToken ct) =>
        File.ReadAllTextAsync(Path.Combine(TemplateDirectory, name), ct);
    // Single pass: user content containing {{...}} is never evaluated as a token.
    private static string Fill(string html, IReadOnlyDictionary<string, string> values) =>
        Regex.Replace(html, @"\{\{(\w+)\}\}", match => values.TryGetValue(match.Groups[1].Value, out var value)
            ? value : throw new InvalidOperationException($"Unknown report token: {match.Groups[1].Value}"));
}
