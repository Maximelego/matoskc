namespace MatosKC.Application.Inspections;

public sealed class SignInspectionUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        Guid id,
        SignInspectionRequest request,
        InspectionActor actor,
        CancellationToken ct = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);

        if (!request.Accepted || string.IsNullOrWhiteSpace(request.SignerName)
            || string.IsNullOrWhiteSpace(request.SignaturePngBase64)
            || request.SignaturePngBase64.Length > 1_400_000)
        {
            throw new InspectionOperationException(400, "Name, explicit acceptance and a PNG signature are required.");
        }

        byte[] png;

        try
        {
            png = Convert.FromBase64String(request.SignaturePngBase64);
        }
        catch (FormatException)
        {
            throw new InspectionOperationException(400, "Signature must be a base64 PNG image.");
        }

        context.SignatureValidator.Validate(png);

        return context.UnitOfWork.ExecuteAsync(async token =>
        {
            var (inspection, rental) = await context.GetInspectionAsync(id, actor, token);
            inspection.Sign(request.SignerName, png, context.Time.GetUtcNow());
            await context.Inspections.UpdateInspectionAsync(inspection, token);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, ct);
    }
}

public sealed class DownloadInspectionReportUseCase(InspectionWorkflowContext context)
{
    public async Task<byte[]> ExecuteAsync(Guid id, InspectionActor actor, CancellationToken ct = default)
    {
        var (inspection, _) = await context.GetInspectionAsync(id, actor, ct);

        return inspection.ReportPdf
            ?? throw new InspectionOperationException(409, "No final report is available for this inspection.");
    }
}

public sealed class DownloadInspectionSignatureUseCase(InspectionWorkflowContext context)
{
    public async Task<byte[]> ExecuteAsync(Guid id, InspectionActor actor, CancellationToken ct = default)
    {
        var (inspection, _) = await context.GetInspectionAsync(id, actor, ct);

        return inspection.SignaturePng
            ?? throw new InspectionOperationException(404, "No signature is attached to this inspection.");
    }
}
