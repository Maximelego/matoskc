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
                    || answer.ExistingDefectId == Guid.Empty
                    || (answer.Result == InspectionResult.Compliant && (!string.IsNullOrWhiteSpace(answer.Observation) || answer.ExistingDefectId.HasValue)))
                || request.Answers.Select(answer => answer.TemplatePointId).Distinct().Count() != request.Answers.Count)
            {
                throw new InspectionOperationException(400, "Answers must reference distinct template points and provide a valid result.");
            }

            foreach (var answer in request.Answers.Where(answer => answer.ExistingDefectId.HasValue))
            {
                await MatosKC.Application.Maintenance.RecordInspectionDefects.RequireOpenAsync(
                    context.Care, answer.ExistingDefectId!.Value, rental.EquipmentId, ct);
            }

            inspection.UpdateOperatorFirstName(request.OperatorFirstName);

            foreach (var answer in request.Answers)
            {
                inspection.SetAnswer(answer.TemplatePointId, answer.Result!.Value, answer.Observation);
                inspection.LinkAnswerToExistingDefect(answer.TemplatePointId, answer.ExistingDefectId);
            }

            OpenInspectionDefects.EnsureConsistent(inspection, template,
                await OpenInspectionDefects.ListAsync(context, rental.EquipmentId, ct));

            await context.Inspections.UpdateInspectionAsync(inspection, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
