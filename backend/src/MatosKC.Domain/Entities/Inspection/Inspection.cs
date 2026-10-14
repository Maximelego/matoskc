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

        foreach (InspectionAnswer answer in _answers)
        {
            if (answer.Result != InspectionResult.NonCompliant)
            {
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

            return;
        }

        _answers.Add(new InspectionAnswer(
            Guid.NewGuid(),
            Id,
            templatePointId,
            result,
            comment
        ));
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
    }

    public void UpdateOperatorFirstName(string firstName)
    {
        EnsureDraft();
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("Operator first name cannot be null or whitespace.", nameof(firstName));
        }

        OperatorFirstName = firstName.Trim();
    }

    private void EnsureDraft()
    {
        if (Status != InspectionStatus.Draft)
        {
            throw new InvalidOperationException(
                "A validated inspection cannot be modified."
            );
        }
    }
}
