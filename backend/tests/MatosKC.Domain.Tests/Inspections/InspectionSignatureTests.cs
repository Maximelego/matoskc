namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Clients;

public sealed class InspectionSignatureTests
{
    [Fact]
    public void Validation_RequiresSignatureEvenWhenAllPointsAreCompliant()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);

        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, InspectionTestData.StartedAt));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Null(inspection.ValidatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ChangingSignedContent_InvalidatesSignature(int change)
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        var point = template.Points[0].Id;
        inspection.SetAnswer(point, InspectionResult.Compliant, null);
        inspection.Sign(" Client ", [1, 2], InspectionTestData.StartedAt);

        if (change == 0)
        {
            inspection.SetAnswer(point, InspectionResult.NonCompliant, "Défaut");
        }
        else if (change == 1)
        {
            inspection.UpdateOperatorFirstName("Autre");
        }
        else
        {
            inspection.AddAnswerPhoto(inspection.Answers[0].Id, Guid.NewGuid());
        }

        Assert.Null(inspection.SignaturePng);
        Assert.Null(inspection.SignerName);
        Assert.Null(inspection.SignedAt);
        Assert.Null(inspection.AcceptanceText);
    }

    [Fact]
    public void SignatureAndFinalReport_AreDefensivelyCopiedAndImmutable()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        byte[] signature = [1, 2];
        inspection.Sign(" Client ", signature, InspectionTestData.StartedAt);
        signature[0] = 9;
        inspection.SignaturePng![0] = 8;
        Assert.Equal(1, inspection.SignaturePng![0]);
        Assert.Equal("Client", inspection.SignerName);
        inspection.Validate(template, InspectionTestData.StartedAt);
        byte[] report = [37, 80, 68, 70];
        inspection.AttachReport(report);
        report[0] = 0;
        inspection.ReportPdf![0] = 0;
        Assert.Equal(37, inspection.ReportPdf![0]);
        Assert.Throws<InvalidOperationException>(() => inspection.Sign("Other", [1], InspectionTestData.StartedAt));
        Assert.Throws<InvalidOperationException>(() => inspection.AttachReport([1]));
    }

    [Fact]
    public void CompliantObservation_IsRejectedWithoutChangingAnExistingAnswer()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.NonCompliant, "Défaut");
        Assert.Throws<ArgumentException>(() => inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, "Défaut"));
        Assert.Equal(InspectionResult.NonCompliant, inspection.Answers[0].Result);
        Assert.Equal("Défaut", inspection.Answers[0].Comment);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("000123")]
    public void CommandNumber_RequiresExactlySixDigits(string value)
    {
        Assert.Equal(value, new CommandNumber(83, value).Value);
        Assert.Throws<ArgumentException>(() => new CommandNumber(83, "12345"));
    }
}
