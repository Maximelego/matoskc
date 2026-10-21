namespace MatosKC.Application.Tests.Inspections;

using MatosKC.Application.Inspections;
using MatosKC.Application.Maintenance;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Inspection;

public sealed class DefectAttributionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitialAuthorAndInspectionStayUnchanged_WhenAnotherOperatorRecordsWorsening(bool useFinding)
    {
        var fake = new WorkflowFake();
        var departure = await fake.DepartureAsync();
        var point = departure.Answers[0].TemplatePointId;
        departure.UpdateOperatorFirstName(" Maxime ");
        departure.SetAnswer(point, InspectionResult.NonCompliant, useFinding ? null : "Rayure");
        if (useFinding)
        {
            var id = Guid.NewGuid();
            departure.SetFinding(id, point, "Rayure");
            departure.AddEvidencePhoto(id, Guid.NewGuid());
        }
        else departure.AddAnswerPhoto(departure.Answers[0].Id, Guid.NewGuid());
        departure.Sign("Client", [1], fake.Clock.Now);
        await new ValidateDepartureInspectionUseCase(fake.Context).ExecuteAsync(departure.Id, fake.Actor);
        var defect = Assert.Single(fake.Care.Defects);
        Assert.Equal("Maxime", defect.OperatorFirstName);
        Assert.Equal(fake.Actor.AccountId, defect.ReportedByAccountId);
        Assert.Equal(departure.Id, defect.InspectionId);
        var original = Assert.Single(fake.Care.Occurrences);
        Assert.Equal("Maxime", original.OperatorFirstName);
        Assert.Equal(departure.Id, original.InspectionId);

        var other = new InspectionActor(Guid.NewGuid(), Role.SuperAdmin, null);
        var result = await new CreateReturnInspectionUseCase(fake.Context).ExecuteAsync(departure.RentalId,
            new(" Pauline "), other);
        var inspection = fake.Inspections.Single(value => value.Id == result.Id);
        inspection.SetAnswer(point, InspectionResult.Compliant, null);
        inspection.ReviewDefect(defect.Id, DefectReviewResult.Worsened, "Rayure agrandie");
        inspection.AddEvidencePhoto(inspection.KnownDefects[0].Id, Guid.NewGuid());
        inspection.Sign("Client", [1], fake.Clock.Now);
        await new ValidateReturnInspectionUseCase(fake.Context).ExecuteAsync(inspection.Id, other);

        Assert.Equal("Maxime", defect.OperatorFirstName);
        Assert.Equal(departure.Id, defect.InspectionId);
        var latest = Assert.Single(fake.Care.Occurrences.Where(value => value.InspectionId == inspection.Id));
        Assert.Equal("Pauline", latest.OperatorFirstName);
        Assert.Equal(other.AccountId, latest.ReportedByAccountId);
        var response = Assert.Single((await new QueryEquipmentCareUseCase(fake.Context).ListDefectsAsync(
            fake.Equipment.Id, null, 0, 50, fake.Actor)).Items);
        Assert.Equal("Maxime", response.OperatorFirstName);
        Assert.Equal(departure.Id, response.InspectionId);
        Assert.Contains(response.Occurrences, value => value.OperatorFirstName == "Pauline"
            && value.InspectionId == inspection.Id && value.ReportedByAccountId == other.AccountId);
    }
}
