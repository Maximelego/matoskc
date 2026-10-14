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
                    || (answer.Result == InspectionResult.Compliant && !string.IsNullOrWhiteSpace(answer.Observation)))
                || request.Answers.Select(answer => answer.TemplatePointId).Distinct().Count() != request.Answers.Count)
            {
                throw new InspectionOperationException(400, "Answers must reference distinct template points and provide a valid result.");
            }

            inspection.UpdateOperatorFirstName(request.OperatorFirstName);

            foreach (var answer in request.Answers)
            {
                inspection.SetAnswer(answer.TemplatePointId, answer.Result!.Value, answer.Observation);
            }

            await context.Inspections.UpdateInspectionAsync(inspection, ct);

            return InspectionWorkflowContext.Response(inspection, rental);
        }, cancellationToken);
    }
}
