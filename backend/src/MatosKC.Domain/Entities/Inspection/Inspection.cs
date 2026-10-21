namespace MatosKC.Domain.Entities.Inspection;

using MatosKC.Domain.Entities;

public class Inspection : BaseEntity
{
    private Inspection()
    {
        OperatorFirstName = null!;
    }


    public Guid RentalId { get; private set; }
    public Guid TemplateId { get; private set; }
    public InspectionType Type { get; private set; }
    public InspectionStatus Status { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public string OperatorFirstName { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ValidatedAt { get; private set; }

    public string? SignerName { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public string? AcceptanceText { get; private set; }

    private byte[]? _signaturePng;
    private byte[]? _reportPdf;

    public byte[]? SignaturePng => _signaturePng?.ToArray();
    public byte[]? ReportPdf => _reportPdf?.ToArray();
    public bool HasReport => _reportPdf is not null;

    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByAccountId { get; private set; }
    public string? CancellationReason { get; private set; }

    public void Cancel(Guid accountId, string reason, DateTimeOffset cancelledAt)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (accountId == Guid.Empty || cancelledAt < StartedAt || reason.Length > 1000)
        {
            throw new ArgumentException("Invalid cancellation account, date or reason.");
        }

        InvalidateSignature();
        CancelledAt = cancelledAt;
        CancelledByAccountId = accountId;
        CancellationReason = reason.Trim();
        Status = InspectionStatus.Cancelled;
    }

    public void Sign(string signerName, byte[] png, DateTimeOffset signedAt)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(signerName);
        ArgumentNullException.ThrowIfNull(png);

        if (png.Length == 0 || signedAt < StartedAt)
        {
            throw new ArgumentException("A signature and a valid signing date are required.");
        }

        SignerName = signerName.Trim();
        SignedAt = signedAt;
        AcceptanceText = "Je reconnais avoir pris connaissance du présent état des lieux et en accepte le contenu.";
        _signaturePng = png.ToArray();
    }

    public void AttachReport(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        if (Status != InspectionStatus.Validated || _reportPdf is not null || pdf.Length == 0)
        {
            throw new InvalidOperationException("A report can only be attached once, after validation.");
        }

        _reportPdf = pdf.ToArray();
    }

    private void InvalidateSignature()
    {
        SignerName = null;
        SignedAt = null;
        AcceptanceText = null;
        _signaturePng = null;
    }

    private readonly List<InspectionDefectReview> _knownDefects = [];
    private readonly List<InspectionFinding> _findings = [];
    public IReadOnlyList<InspectionDefectReview> KnownDefects => _knownDefects.AsReadOnly();
    public IReadOnlyList<InspectionFinding> Findings => _findings.Where(value => !value.IsRemoved).ToArray();

    public void PresentDefect(Guid defectId, string description, string observation,
        DateTimeOffset observedAt, IEnumerable<Guid> historicalPhotos)
    {
        EnsureDraft();
        var existing = _knownDefects.SingleOrDefault(value => value.DefectId == defectId);
        if (existing is not null)
        {
            if (existing.Refresh(description, observation, observedAt, historicalPhotos)) InvalidateSignature();
            return;
        }
        _knownDefects.Add(new InspectionDefectReview(Id, defectId, description, observation, observedAt, historicalPhotos));
        InvalidateSignature();
    }

    public void ReviewDefect(Guid defectId, DefectReviewResult result, string? observation)
    {
        EnsureDraft();
        var review = _knownDefects.SingleOrDefault(value => value.DefectId == defectId)
            ?? throw new ArgumentException("Defect was not presented in this inspection.");
        review.Update(result, observation); InvalidateSignature();
    }

    public void SetFinding(Guid id, Guid pointId, string observation)
    {
        EnsureDraft();
        if (_knownDefects.Any(value => value.Id == id) || _answers.Any(value => value.Id == id))
            throw new ArgumentException("Finding identifier is already used.");
        var existing = _findings.SingleOrDefault(value => value.Id == id);
        if (existing is not null)
        {
            if (existing.IsRemoved) throw new ArgumentException("A removed finding identifier cannot be reused.");
            if (existing.TemplatePointId != pointId) throw new ArgumentException("A finding cannot change its template point.");
            existing.Update(observation);
        }
        else _findings.Add(new InspectionFinding(id, Id, pointId, observation));
        InvalidateSignature();
    }

    public void RemoveFinding(Guid id)
    {
        EnsureDraft();
        (_findings.SingleOrDefault(value => value.Id == id)
            ?? throw new ArgumentException("Finding not found.")).Remove();
        InvalidateSignature();
    }

    public bool HasEvidenceTarget(Guid id) => _findings.Any(value => value.Id == id && !value.IsRemoved)
        || _knownDefects.Any(value => value.Id == id);

    public void AddEvidencePhoto(Guid targetId, Guid photoId)
    {
        EnsureDraft();
        var finding = _findings.SingleOrDefault(value => value.Id == targetId && !value.IsRemoved);
        if (finding is not null) finding.AddPhoto(photoId);
        else (_knownDefects.SingleOrDefault(value => value.Id == targetId)
            ?? throw new ArgumentException("Evidence target not found.")).AddPhoto(photoId);
        InvalidateSignature();
    }

    public void RemoveEvidencePhoto(Guid targetId, Guid photoId)
    {
        EnsureDraft();
        var finding = _findings.SingleOrDefault(value => value.Id == targetId && !value.IsRemoved);
        if (finding is not null) finding.RemovePhoto(photoId);
        else (_knownDefects.SingleOrDefault(value => value.Id == targetId)
            ?? throw new ArgumentException("Evidence target not found.")).RemovePhoto(photoId);
        InvalidateSignature();
    }

    private readonly List<InspectionAnswer> _answers = [];

    public IReadOnlyList<InspectionAnswer> Answers =>
        _answers.AsReadOnly();

    public Inspection(
        Guid id,
        Guid rentalId,
        Guid templateId,
        InspectionType type,
        Guid createdByAccountId,
        string operatorFirstName,
        DateTimeOffset startedAt
    ) : base(id)
    {
        if (rentalId == Guid.Empty)
            throw new ArgumentException("Rental ID cannot be empty.", nameof(rentalId));

        if (templateId == Guid.Empty)
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));

        if (string.IsNullOrWhiteSpace(operatorFirstName))
            throw new ArgumentException("Operator first name cannot be null or whitespace.", nameof(operatorFirstName));

        if (createdByAccountId == Guid.Empty)
            throw new ArgumentException("Created by account ID cannot be empty.", nameof(createdByAccountId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid inspection type.", nameof(type));

        RentalId = rentalId;
        TemplateId = templateId;
        Type = type;
        Status = InspectionStatus.Draft;
        CreatedByAccountId = createdByAccountId;
        OperatorFirstName = operatorFirstName.Trim();
        StartedAt = startedAt;
    }

    public void Validate(
        InspectionTemplate template,
        DateTimeOffset validatedAt
    )
    {
        EnsureDraft();

        ArgumentNullException.ThrowIfNull(template);

        if (template.Id != TemplateId)
        {
            throw new ArgumentException(
                "Template does not match this inspection.",
                nameof(template)
            );
        }

        if (validatedAt < StartedAt)
        {
            throw new ArgumentException(
                "Validation date cannot be earlier than the start date.",
                nameof(validatedAt)
            );
        }

        HashSet<Guid> expectedPointIds = [.. template.Points.Select(point => point.Id)];

        HashSet<Guid> answeredPointIds = [.. _answers.Select(answer => answer.TemplatePointId)];

        if (_answers.Count != answeredPointIds.Count)
        {
            throw new InvalidOperationException(
                "Each inspection point must have only one answer."
            );
        }

        if (!expectedPointIds.SetEquals(answeredPointIds))
        {
            throw new InvalidOperationException(
                "Every template point must be answered, without additional points."
            );
        }

        if (_answers.Any(answer => answer.ExistingDefectId.HasValue))
            throw new InvalidOperationException("Review existing defects independently from questionnaire answers.");
        foreach (var review in _knownDefects) review.EnsureComplete();
        foreach (var finding in Findings)
        {
            if (!expectedPointIds.Contains(finding.TemplatePointId) || finding.PhotoIds.Count == 0
                || !_answers.Any(answer => answer.TemplatePointId == finding.TemplatePointId && answer.Result == InspectionResult.NonCompliant))
                throw new InvalidOperationException("Every new finding requires a non-compliant answer and a photo.");
        }

        foreach (InspectionAnswer answer in _answers)
        {
            if (answer.Result != InspectionResult.NonCompliant)
            {
                continue;
            }

            if (Findings.Any(value => value.TemplatePointId == answer.TemplatePointId))
            {
                if (!string.IsNullOrWhiteSpace(answer.Comment) || answer.PhotosIds.Count > 0 || answer.ExistingDefectId.HasValue)
                    throw new InvalidOperationException("Use either findings or legacy answer evidence, not both.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(answer.Comment))
            {
                throw new InvalidOperationException(
                    "A non-compliant answer requires a comment."
                );
            }

            if (answer.PhotosIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "A non-compliant answer requires at least one photo."
                );
            }
        }

        if (_signaturePng is null || SignedAt is null || SignedAt > validatedAt)
        {
            throw new InvalidOperationException("A signed inspection is required before validation.");
        }

        Status = InspectionStatus.Validated;
        ValidatedAt = validatedAt;
    }

    public void SetAnswer(
        Guid templatePointId,
        InspectionResult result,
        string? comment
    )
    {
        EnsureDraft();

        if (templatePointId == Guid.Empty)
        {
            throw new ArgumentException("Template point ID cannot be empty.", nameof(templatePointId));
        }

        if (!Enum.IsDefined(result))
        {
            throw new ArgumentException("Invalid inspection result.", nameof(result));
        }

        InspectionAnswer? existingAnswer = _answers.SingleOrDefault(
            answer => answer.TemplatePointId == templatePointId
        );

        if (existingAnswer is not null)
        {
            existingAnswer.UpdateResult(result, comment);
            InvalidateSignature();

            return;
        }

        _answers.Add(new InspectionAnswer(
            Guid.NewGuid(),
            Id,
            templatePointId,
            result,
            comment
        ));
        InvalidateSignature();
    }

    public void LinkAnswerToExistingDefect(Guid templatePointId, Guid? defectId)
    {
        EnsureDraft();
        var answer = _answers.SingleOrDefault(value => value.TemplatePointId == templatePointId)
            ?? throw new InvalidOperationException("Answer not found.");

        answer.LinkExistingDefect(defectId);
        InvalidateSignature();
    }

    public void AddAnswerPhoto(Guid answerId, Guid photoId)
    {
        EnsureDraft();

        if (answerId == Guid.Empty)
        {
            throw new ArgumentException("Answer ID cannot be empty.", nameof(answerId));
        }

        if (!_answers.Any(a => a.Id == answerId))
        {
            throw new ArgumentException("Answer not found.", nameof(answerId));
        }

        _answers.First(a => a.Id == answerId).AddPhoto(photoId);
        InvalidateSignature();
    }

    public void RemoveAnswerPhoto(Guid answerId, Guid photoId)
    {
        EnsureDraft();

        if (answerId == Guid.Empty)
        {
            throw new ArgumentException("Answer ID cannot be empty.", nameof(answerId));
        }

        if (!_answers.Any(a => a.Id == answerId))
        {
            throw new ArgumentException("Answer not found.", nameof(answerId));
        }

        _answers.First(a => a.Id == answerId).RemovePhoto(photoId);
        InvalidateSignature();
    }

    public void UpdateOperatorFirstName(string firstName)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("Operator first name cannot be null or whitespace.", nameof(firstName));
        }

        OperatorFirstName = firstName.Trim();
        InvalidateSignature();
    }

    private void EnsureDraft()
    {
        if (Status != InspectionStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only a draft inspection can be modified."
            );
        }
    }
}
