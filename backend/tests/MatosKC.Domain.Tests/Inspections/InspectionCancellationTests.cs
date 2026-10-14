namespace MatosKC.Domain.Tests.Inspections;

using MatosKC.Domain.Entities.Clients;
using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;

public sealed class InspectionCancellationTests
{
    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("ABCDEF")]
    [InlineData("12 456")]
    [InlineData("１２３４５６")]
    [InlineData("١٢٣٤٥٦")]
    public void CommandNumber_RejectsAnythingExceptSixAsciiDigits(string value)
    {
        Assert.Throws<ArgumentException>(() => new CommandNumber(83, value));
        Assert.Equal("000123", new CommandNumber(83, "000123").Value);
    }

    [Fact]
    public void Cancellation_KeepsAnswersAndAuditButRemovesSignatureAndFreezesInspection()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null);
        inspection.Sign("Client", [1], InspectionTestData.StartedAt);
        Guid account = Guid.NewGuid();
        var now = InspectionTestData.StartedAt.AddMinutes(1);
        inspection.Cancel(account, " Abandon ", now);

        Assert.Equal(InspectionStatus.Cancelled, inspection.Status);
        Assert.Equal(account, inspection.CancelledByAccountId);
        Assert.Equal(now, inspection.CancelledAt);
        Assert.Equal("Abandon", inspection.CancellationReason);
        Assert.Single(inspection.Answers);
        Assert.Null(inspection.SignaturePng);
        Assert.Throws<InvalidOperationException>(() => inspection.SetAnswer(template.Points[0].Id, InspectionResult.Compliant, null));
        Assert.Throws<InvalidOperationException>(() => inspection.Sign("Client", [1], now));
        Assert.Throws<InvalidOperationException>(() => inspection.Validate(template, now));
    }

    [Fact]
    public void Cancellation_RejectsInvalidInputWithoutChangingStatus()
    {
        var template = InspectionTestData.Template();
        var inspection = InspectionTestData.Inspection(template);
        Assert.Throws<ArgumentException>(() => inspection.Cancel(Guid.Empty, "Abandon", InspectionTestData.StartedAt));
        Assert.Throws<ArgumentException>(() => inspection.Cancel(Guid.NewGuid(), "Abandon", InspectionTestData.StartedAt.AddTicks(-1)));
        Assert.Throws<ArgumentException>(() => inspection.Cancel(Guid.NewGuid(), " ", InspectionTestData.StartedAt));
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        var rental = new Rental(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionTestData.StartedAt);
        Assert.Throws<ArgumentException>(() => rental.Cancel(InspectionTestData.StartedAt.AddTicks(-1)));
        rental.Cancel(InspectionTestData.StartedAt);
        Assert.Equal(RentalStatus.Cancelled, rental.Status);
        Assert.Throws<InvalidOperationException>(() => rental.Cancel(InspectionTestData.StartedAt));
    }
}
