namespace MatosKC.Infrastructure.Reports;

using MatosKC.Application.Agencies.Ports;
using MatosKC.Application.Clients.Ports;
using MatosKC.Application.Equipments.Ports;
using MatosKC.Application.Files.Ports;
using MatosKC.Application.Inspections;
using MatosKC.Application.Inspections.Ports;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
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
    public async Task<byte[]> GenerateAsync(
        InspectionEntity inspection,
        Rental rental,
        InspectionTemplate template,
        CancellationToken ct = default
    )
    {
        if (inspection.Status != InspectionStatus.Validated || inspection.SignaturePng is null)
        {
            throw new InvalidOperationException("Only a signed, validated inspection can be rendered.");
        }

        var client = await clients.GetByIdAsync(rental.ClientId, ct)
            ?? throw new InspectionOperationException(404, "Client not found.");
        var item = await equipment.RetrieveByIdAsync(rental.EquipmentId, ct)
            ?? throw new InspectionOperationException(404, "Equipment not found.");
        var agency = rental.AgencyId.HasValue
            ? await agencies.GetByIdAsync(rental.AgencyId.Value, ct)
            : null;

        using var report = new ReportCanvas();
        report.Text("MatosKC - Etat des lieux", 18);
        report.Text(inspection.Type == InspectionType.Departure ? "Départ" : "Retour", 14);
        report.Text($"Agence : {agency?.Name ?? "Historique"} ({client.CommandNumber.AgencyCode})");
        report.Text($"Matériel : {item.Name} - {item.SerialNumber}");
        report.Text($"Location : {rental.Id}");
        report.Text($"Commande : {client.CommandNumber.Value}");
        report.Text($"Client : {client.DisplayName}");
        report.Text($"Contact : {client.Email} - {client.Phone}");
        report.Text($"Opérateur : {inspection.OperatorFirstName}");
        report.Text($"EDL : {inspection.Id} - Template : {template.Id}");
        report.Text($"Début : {inspection.StartedAt:O}");
        report.Text($"Validation : {inspection.ValidatedAt:O}");

        foreach (var point in template.Points.OrderBy(value => value.Position))
        {
            ct.ThrowIfCancellationRequested();
            var answer = inspection.Answers.Single(value => value.TemplatePointId == point.Id);
            report.Text(point.Description, 12);
            report.Text(answer.Result == InspectionResult.Compliant ? "Conforme" : "Non conforme");

            if (answer.Comment is not null)
            {
                report.Text(answer.Comment);
            }

            foreach (Guid photoId in answer.PhotosIds)
            {
                var photo = await media.GetByIdAsync(photoId, ct)
                    ?? throw new InspectionOperationException(409, "An inspection photo is missing.");
                var content = await storage.RetrieveAsync(photo.ObjectKey, ct)
                    ?? throw new InspectionOperationException(409, "An inspection photo is unavailable.");
                await using var source = content.Content;
                await using var bytes = new MemoryStream();
                await source.CopyToAsync(bytes, ct);
                report.Image(bytes.ToArray(), 210);
            }
        }

        report.Reserve(180);
        report.Text(inspection.AcceptanceText!);
        report.Text($"Signataire : {inspection.SignerName} - {inspection.SignedAt:O}");
        report.Image(inspection.SignaturePng, 100);

        return report.Save();
    }

    // Embedded font makes the report independent of the host's installed fonts.
    private sealed class ReportFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            using var source = typeof(InspectionReportGenerator).Assembly.GetManifestResourceStream(
                "MatosKC.Infrastructure.Reports.Fonts.DejaVuSans.ttf")!;
            using var target = new MemoryStream();
            source.CopyTo(target);

            return target.ToArray();
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new("DejaVuSans");
    }

    private sealed class ReportCanvas : IDisposable
    {
        private static readonly object FontLock = new();
        private readonly PdfDocument _document = new();
        private XGraphics? _graphics;
        private double _y;
        private const double Width = 515;

        public ReportCanvas()
        {
            lock (FontLock)
            {
                GlobalFontSettings.FontResolver ??= new ReportFontResolver();
            }

            _document.Info.Title = "MatosKC - Etat des lieux";
            NewPage();
        }

        private void NewPage()
        {
            _graphics?.Dispose();
            var page = _document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            _graphics = XGraphics.FromPdfPage(page);
            _y = 40;
            _graphics.DrawString($"MatosKC - {_document.PageCount}", new XFont("DejaVuSans", 8),
                XBrushes.Gray, new XPoint(40, 820));
        }

        private void EnsureSpace(double height)
        {
            if (_y + height > 790)
            {
                NewPage();
            }
        }

        public void Reserve(double height) => EnsureSpace(height);

        public void Text(string text, double size = 10)
        {
            var font = new XFont("DejaVuSans", size);

            foreach (string paragraph in text.Replace("\r", "").Split('\n'))
            {
                string line = "";

                foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    string candidate = line.Length == 0 ? word : line + " " + word;

                    if (_graphics!.MeasureString(candidate, font).Width <= Width)
                    {
                        line = candidate;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        DrawLine(line, font, size);
                    }

                    line = "";

                    // Only an unbroken word wider than the page is split into characters.
                    foreach (char value in word)
                    {
                        if (_graphics!.MeasureString(line + value, font).Width > Width)
                        {
                            DrawLine(line, font, size);
                            line = "";
                        }

                        line += value;
                    }
                }

                DrawLine(line, font, size);
            }

            _y += 5;
        }

        private void DrawLine(string line, XFont font, double size)
        {
            EnsureSpace(size + 5);
            _graphics!.DrawString(line, font, XBrushes.Black, new XPoint(40, _y + size));
            _y += size + 5;
        }

        public void Image(byte[] bytes, double maxHeight)
        {
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);

            if (codec is null || (long)codec.Info.Width * codec.Info.Height > 25_000_000)
            {
                throw new InspectionOperationException(422, "A report image is invalid or too large.");
            }

            using var bitmap = SKBitmap.Decode(bytes);

            if (bitmap is null)
            {
                throw new InspectionOperationException(422, "A report image cannot be decoded.");
            }

            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(png.ToArray());
            using var image = XImage.FromStream(stream);
            double scale = Math.Min(Width / image.PixelWidth, maxHeight / image.PixelHeight);
            double height = image.PixelHeight * scale;
            EnsureSpace(height + 12);
            _graphics!.DrawImage(image, 40, _y, image.PixelWidth * scale, height);
            _y += height + 12;
        }

        public byte[] Save()
        {
            _graphics?.Dispose();
            _graphics = null;
            using var stream = new MemoryStream();
            _document.Save(stream, false);

            return stream.ToArray();
        }

        public void Dispose()
        {
            _graphics?.Dispose();
            _document.Dispose();
        }
    }
}
