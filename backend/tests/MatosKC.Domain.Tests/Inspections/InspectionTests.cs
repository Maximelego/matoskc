namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public class InspectionTests
{
    [Fact]
    public void NewInspection_IsAnEmptyDraftAndTrimsOperatorName()
    {
        var inspection = new InspectionEntity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionType.Departure,
            Guid.NewGuid(), " Maxime ", InspectionTestData.StartedAt
        );

        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Null(inspection.ValidatedAt);
        Assert.Empty(inspection.Answers);
        Assert.Equal("Maxime", inspection.OperatorFirstName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Constructor_RejectsEmptyIdentifiers(int emptyIndex)
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        ids[emptyIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() => new InspectionEntity(
            ids[0], ids[1], ids[2], InspectionType.Departure, ids[3], "Maxime",
            InspectionTestData.StartedAt
        ));
    }

    [Fact]
    public void Constructor_RejectsUnknownType()
    {
        Assert.Throws<ArgumentException>(() => new InspectionEntity(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (InspectionType)99,
            Guid.NewGuid(), "Maxime", InspectionTestData.StartedAt
        ));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OperatorName_RejectsBlankValues(string? name)
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);

        Assert.Throws<ArgumentException>(() => new InspectionEntity(
            Guid.NewGuid(), Guid.NewGuid(), template.Id, InspectionType.Departure,
            Guid.NewGuid(), name!, InspectionTestData.StartedAt
        ));
        Assert.Throws<ArgumentException>(() => inspection.UpdateOperatorFirstName(name!));
        Assert.Equal("Maxime", inspection.OperatorFirstName);
    }

    [Fact]
    public void UpdatingAnswer_PreservesItsIdentityAndPhotos()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        Guid pointId = template.Points[0].Id;
        inspection.SetAnswer(pointId, InspectionResult.NonCompliant, "Défaut");
        var answer = Assert.Single(inspection.Answers);
        Guid photoId = Guid.NewGuid();
        inspection.AddAnswerPhoto(answer.Id, photoId);

        inspection.SetAnswer(pointId, InspectionResult.NonCompliant, " Défaut précisé ");

        var updated = Assert.Single(inspection.Answers);
        Assert.Same(answer, updated);
        Assert.Equal(answer.Id, updated.Id);
        Assert.Equal("Défaut précisé", updated.Comment);
        Assert.Equal(photoId, Assert.Single(updated.PhotosIds));
    }

    [Fact]
    public void Draft_AllowsNonCompliantAnswerWithoutCommentOrPhoto()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, null);
        var answer = Assert.Single(inspection.Answers);

        Assert.Null(answer.Comment);
        Assert.Empty(answer.PhotosIds);
        Assert.Throws<InvalidOperationException>(() =>
            inspection.Validate(template, InspectionTestData.StartedAt.AddMinutes(5)));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Null(inspection.ValidatedAt);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("foreign")]
    public void Validation_RequiresExactlyTheTemplatePoints(string error)
    {
        var template = InspectionTestData.Template(2);
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);

        if (error == "extra")
        {
            inspection.SetAnswer(template.Points[1].Id, InspectionResult.Compliant, null);
        }

        if (error != "missing")
        {
            inspection.SetAnswer(Guid.NewGuid(), InspectionResult.Compliant, null);
        }

        Assert.Throws<InvalidOperationException>(() =>
            inspection.Validate(template, InspectionTestData.StartedAt.AddMinutes(5)));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Null(inspection.ValidatedAt);
    }

    [Fact]
    public void Validation_RejectsAnEmptyInspection()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);

        Assert.Throws<InvalidOperationException>(() =>
            inspection.Validate(template, InspectionTestData.StartedAt));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonCompliance_RequiresBothCommentAndPhotoAtValidation(bool hasPhoto)
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(
            template.Points[0].Id, InspectionResult.NonCompliant, hasPhoto ? null : "Défaut"
        );

        if (hasPhoto)
        {
            inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());
        }

        Assert.Throws<InvalidOperationException>(() =>
            inspection.Validate(template, InspectionTestData.StartedAt.AddMinutes(5)));
        Assert.Null(inspection.ValidatedAt);
    }

    [Fact]
    public void CompleteNonCompliance_CanBeValidated()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, "Défaut");
        inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());
        var validatedAt = InspectionTestData.StartedAt.AddMinutes(5);

        inspection.Validate(template, validatedAt);

        Assert.Equal(InspectionStatus.Validated, inspection.Status);
        Assert.Equal(validatedAt, inspection.ValidatedAt);
    }

    [Fact]
    public void Validation_RejectsWrongTemplateNullAndEarlierDate()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);

        Assert.Throws<ArgumentNullException>(() =>
            inspection.Validate(null!, InspectionTestData.StartedAt));
        Assert.Throws<ArgumentException>(() =>
            inspection.Validate(InspectionTestData.Template(), InspectionTestData.StartedAt));
        Assert.Throws<ArgumentException>(() =>
            inspection.Validate(template, InspectionTestData.StartedAt.AddTicks(-1)));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
    }

    [Fact]
    public void CompliantInspection_CanBeValidatedAtStartDateWithoutJustification()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);

        inspection.Validate(template, InspectionTestData.StartedAt);

        Assert.Equal(InspectionStatus.Validated, inspection.Status);
        Assert.Equal(InspectionTestData.StartedAt, inspection.ValidatedAt);
    }

    [Theory]
    [InlineData("answer")]
    [InlineData("addPhoto")]
    [InlineData("removePhoto")]
    [InlineData("operator")]
    [InlineData("validate")]
    public void ValidatedInspection_RejectsAllPublicMutations(string operation)
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        var answer = inspection.Answers[0];
        Guid photoId = Guid.NewGuid();
        inspection.AddAnswerPhoto(answer.Id, photoId);
        inspection.Validate(template, InspectionTestData.StartedAt);

        Assert.Throws<InvalidOperationException>(() =>
        {
            switch (operation)
            {
                case "answer":
                    inspection.SetAnswer(answer.TemplatePointId, InspectionResult.NonCompliant, null);
                    break;
                case "addPhoto":
                    inspection.AddAnswerPhoto(answer.Id, Guid.NewGuid());
                    break;
                case "removePhoto":
                    inspection.RemoveAnswerPhoto(answer.Id, photoId);
                    break;
                case "operator":
                    inspection.UpdateOperatorFirstName("Autre");
                    break;
                case "validate":
                    inspection.Validate(template, InspectionTestData.StartedAt.AddMinutes(1));
                    break;
            }
        });

        Assert.Equal(InspectionResult.Compliant, answer.Result);
        Assert.Equal(photoId, Assert.Single(answer.PhotosIds));
        Assert.Equal("Maxime", inspection.OperatorFirstName);
        Assert.Equal(InspectionTestData.StartedAt, inspection.ValidatedAt);
    }

    [Fact]
    public void ReadOnlyCollections_CannotBeClearedEvenAfterCasting()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());

        Assert.Throws<NotSupportedException>(() =>
            ((IList<InspectionAnswer>)inspection.Answers).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<Guid>)inspection.Answers[0].PhotosIds).Clear());
        Assert.Single(inspection.Answers);
        Assert.Single(inspection.Answers[0].PhotosIds);
    }

    [Fact]
    public void Photos_CannotBeDuplicatedAndCanBeRemoved()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        var answer = inspection.Answers[0];
        Guid photoId = Guid.NewGuid();
        inspection.AddAnswerPhoto(answer.Id, photoId);

        Assert.Throws<InvalidOperationException>(() => inspection.AddAnswerPhoto(answer.Id, photoId));
        Assert.Single(answer.PhotosIds);

        inspection.RemoveAnswerPhoto(answer.Id, photoId);

        Assert.Empty(answer.PhotosIds);
    }

    [Fact]
    public void InvalidAnswerUpdate_DoesNotChangeExistingAnswerOrPhotos()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, "Avant");
        var answer = inspection.Answers[0];
        inspection.AddAnswerPhoto(answer.Id, Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            inspection.SetAnswer(answer.TemplatePointId, (InspectionResult)99, "Après"));
        Assert.Throws<ArgumentException>(() =>
            inspection.SetAnswer(Guid.Empty, InspectionResult.Compliant, null));

        Assert.Single(inspection.Answers);
        Assert.Equal("Avant", answer.Comment);
        Assert.Equal(InspectionResult.Compliant, answer.Result);
        Assert.Single(answer.PhotosIds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhotoOperations_RejectUnknownAnswerAndEmptyPhoto(bool remove)
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        Guid answerId = inspection.Answers[0].Id;

        void Act(Guid answer, Guid photo)
        {
            if (remove)
            {
                inspection.RemoveAnswerPhoto(answer, photo);
            }
            else
            {
                inspection.AddAnswerPhoto(answer, photo);
            }
        }

        Assert.Throws<ArgumentException>(() => Act(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => Act(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => Act(answerId, Guid.Empty));
        Assert.Empty(inspection.Answers[0].PhotosIds);
    }
}
