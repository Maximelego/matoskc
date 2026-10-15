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
public sealed record SaveDraftRequest(string OperatorFirstName, IReadOnlyList<SaveAnswerRequest> Answers);

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
    string? CancellationReason
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
