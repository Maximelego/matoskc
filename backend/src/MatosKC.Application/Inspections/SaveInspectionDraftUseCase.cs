namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Inspection;

public sealed class SaveInspectionDraftUseCase(InspectionWorkflowContext context)
{
    public Task<InspectionResponse> ExecuteAsync(
        Guid id,
        SaveDraftRequest request,
        InspectionActor actor,
        CancellationToken cancellationToken = default
    )
    {
        InspectionWorkflowContext.RequireOperator(actor);
        ArgumentNullException.ThrowIfNull(request.Answers);

        return context.UnitOfWork.ExecuteAsync(async ct =>
        {
            var (inspection, rental) = await context.GetInspectionAsync(id, actor, ct);
            var template = await context.Templates.GetTemplateByIdAsync(inspection.TemplateId, ct)
                ?? throw new InspectionOperationException(404, "Template not found.");
            var allowed = template.Points.Select(point => point.Id).ToHashSet();

            if (request.Answers.Any(answer => answer is null || !allowed.Contains(answer.TemplatePointId)
                    || !answer.Result.HasValue || !Enum.IsDefined(answer.Result.Value)
                    || answer.ExistingDefectId.HasValue
                    || (answer.Result == InspectionResult.Compliant && (!string.IsNullOrWhiteSpace(answer.Observation) || answer.ExistingDefectId.HasValue)))
                || request.Answers.Select(answer => answer.TemplatePointId).Distinct().Count() != request.Answers.Count)
            {
                throw new InspectionOperationException(400, "Answers must reference distinct template points and provide a valid result.");
            }

            var reviews = request.KnownDefects ?? [];
            var findings = request.Findings ?? [];
            var removed = request.RemovedFindingIds ?? [];
            if (reviews.Any(value => value is null || !Enum.IsDefined(value.Result))
                || reviews.Select(value => value.DefectId).Distinct().Count() != reviews.Count
                || findings.Any(value => value is null || value.Id == Guid.Empty || !allowed.Contains(value.TemplatePointId)
                    || string.IsNullOrWhiteSpace(value.Observation))
                || findings.Select(value => value.Id).Distinct().Count() != findings.Count
                || removed.Contains(Guid.Empty) || removed.Distinct().Count() != removed.Count
                || findings.Any(value => removed.Contains(value.Id)))
                throw new InspectionOperationException(400, "Invalid or duplicate defect reviews or findings.");

            await OpenInspectionDefects.CaptureAsync(context, inspection, rental.EquipmentId, ct);
            if (reviews.Any(value => !inspection.KnownDefects.Any(review => review.DefectId == value.DefectId))
                || removed.Any(value => !inspection.Findings.Any(finding => finding.Id == value)))
                throw new InspectionOperationException(400, "Unknown defect review or finding.");

            inspection.UpdateOperatorFirstName(request.OperatorFirstName);
            foreach (var review in reviews) inspection.ReviewDefect(review.DefectId, review.Result, review.Observation);
            foreach (var value in removed) inspection.RemoveFinding(value);
            foreach (var finding in findings) inspection.SetFinding(finding.Id, finding.TemplatePointId, finding.Observation);

            foreach (var answer in request.Answers)
            {
                inspection.SetAnswer(answer.TemplatePointId, answer.Result!.Value, answer.Observation);
                inspection.LinkAnswerToExistingDefect(answer.TemplatePointId, answer.ExistingDefectId);
            }

            OpenInspectionDefects.EnsureConsistent(inspection,
                await OpenInspectionDefects.ListAsync(context, rental.EquipmentId, ct));

            await context.Inspections.UpdateInspectionAsync(inspection, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
