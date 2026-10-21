namespace MatosKC.Application.Inspections;

using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;

public sealed record InspectionActor(Guid AccountId, Role Role, int? AgencyCode, Guid? AgencyId = null);

public sealed record ClientInput(
    string DisplayName,
    string Email,
    string Phone,
    int AgencyCode,
    string CommandValue
);

public sealed record CreateDepartureRequest(Guid EquipmentId, ClientInput Client, string OperatorFirstName);

public sealed record CreateReturnRequest(string OperatorFirstName);

public sealed record SaveAnswerRequest(Guid TemplatePointId, InspectionResult? Result, string? Observation, Guid? ExistingDefectId = null);

// A partial update: omitted answers and attached photos are preserved.
public sealed record SaveDefectReviewRequest(Guid DefectId,
    [property: System.Text.Json.Serialization.JsonRequired] DefectReviewResult Result, string? Observation = null);
public sealed record SaveFindingRequest(Guid Id, Guid TemplatePointId, string Observation);

// Collections are partial updates. Explicit finding removals preserve omitted evidence.
public sealed record SaveDraftRequest(string OperatorFirstName, IReadOnlyList<SaveAnswerRequest> Answers,
    IReadOnlyList<SaveDefectReviewRequest>? KnownDefects = null,
    IReadOnlyList<SaveFindingRequest>? Findings = null,
    IReadOnlyList<Guid>? RemovedFindingIds = null);

public sealed record DefectReviewResponse(Guid Id, Guid DefectId, string Description,
    string HistoricalObservation, DateTimeOffset ObservedAt, IReadOnlyList<Guid> HistoricalPhotoIds,
    DefectReviewResult? Result, string? Observation, IReadOnlyList<Guid> PhotoIds);
public sealed record FindingResponse(Guid Id, Guid TemplatePointId, string Observation, IReadOnlyList<Guid> PhotoIds);

public sealed record TemplatePointInput(string Description, string DescriptionDetail, Guid? PhotoId);

public sealed record CreateTemplateRequest(Guid EquipmentCategoryId, IReadOnlyList<TemplatePointInput> Points);

public sealed record InspectionAnswerResponse(
    Guid Id,
    Guid TemplatePointId,
    InspectionResult Result,
    string? Observation,
    IReadOnlyList<Guid> PhotoIds,
    Guid? ExistingDefectId = null
);

public sealed record InspectionResponse(
    Guid Id,
    Guid RentalId,
    Guid EquipmentId,
    Guid TemplateId,
    InspectionType Type,
    InspectionStatus Status,
    Guid CreatedByAccountId,
    string OperatorFirstName,
    DateTimeOffset StartedAt,
    DateTimeOffset? ValidatedAt,
    IReadOnlyList<InspectionAnswerResponse> Answers,
    string? SignerName,
    DateTimeOffset? SignedAt,
    string? AcceptanceText,
    bool HasReport,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByAccountId,
    string? CancellationReason,
    IReadOnlyList<DefectReviewResponse> KnownDefects,
    IReadOnlyList<FindingResponse> Findings
);

public sealed record SignInspectionRequest(string SignerName, string SignaturePngBase64, bool Accepted);

public sealed record RentalResponse(
    Guid Id,
    Guid ClientId,
    Guid EquipmentId,
    RentalStatus Status,
    DateTimeOffset CreatedAt,
    Guid? DepartureInspectionId,
    Guid? ReturnInspectionId,
    DateTimeOffset? DepartedAt,
    DateTimeOffset? ReturnedAt,
    ClientInput Client,
    DateTimeOffset? CancelledAt
);

public sealed class InspectionOperationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
