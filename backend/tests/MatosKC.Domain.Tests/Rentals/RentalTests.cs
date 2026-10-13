namespace MatosKC.Domain.Tests.Rentals;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using MatosKC.Domain.Tests.Inspections;
using RentalEntity = MatosKC.Domain.Entities.Rental.Rental;

public class RentalTests
{
    private static RentalEntity CreateRental() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionTestData.StartedAt
    );

    [Fact]
    public void Constructor_CreatesDraftWithoutInspectionsOrDepartureDates()
    {
        var rental = CreateRental();

        Assert.Equal(RentalStatus.Draft, rental.Status);
        Assert.Null(rental.DepartureInspectionId);
        Assert.Null(rental.ReturnInspectionId);
        Assert.Null(rental.DepartedAt);
        Assert.Null(rental.ReturnedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Constructor_RejectsEmptyIdentifiers(int emptyIndex)
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
        ids[emptyIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() => new RentalEntity(
            ids[0], ids[1], ids[2], InspectionTestData.StartedAt
        ));
    }

    [Fact]
    public void ConfirmDeparture_AssociatesValidatedInspectionAndActivatesRental()
    {
        var rental = CreateRental();
        var template = InspectionTestData.Template();
        var departure = InspectionTestData.Validated(template, rental.Id, InspectionType.Departure);
        var departedAt = InspectionTestData.StartedAt.AddMinutes(10);

        rental.ConfirmDeparture(departure, departedAt);

        Assert.Equal(RentalStatus.Active, rental.Status);
        Assert.Equal(departure.Id, rental.DepartureInspectionId);
        Assert.Equal(departedAt, rental.DepartedAt);
        Assert.Null(rental.ReturnInspectionId);
    }

    [Fact]
    public void ConfirmReturn_AssociatesInspectionAndAllowsDifferentTemplate()
    {
        var rental = CreateRental();
        var departure = InspectionTestData.Validated(
            InspectionTestData.Template(), rental.Id, InspectionType.Departure
        );
        var returnInspection = InspectionTestData.Validated(
            InspectionTestData.Template(2), rental.Id, InspectionType.Return
        );
        var returnedAt = InspectionTestData.StartedAt.AddHours(1);

        rental.ConfirmDeparture(departure, InspectionTestData.StartedAt.AddMinutes(10));
        rental.ConfirmReturn(returnInspection, returnedAt);

        Assert.NotEqual(departure.TemplateId, returnInspection.TemplateId);
        Assert.Equal(RentalStatus.Completed, rental.Status);
        Assert.Equal(returnInspection.Id, rental.ReturnInspectionId);
        Assert.Equal(returnedAt, rental.ReturnedAt);
    }

    [Fact]
    public void SecondDeparture_IsRejectedWithoutChangingFirstDeparture()
    {
        var rental = CreateRental();
        var departure = InspectionTestData.Validated(
            InspectionTestData.Template(), rental.Id, InspectionType.Departure
        );
        var departedAt = InspectionTestData.StartedAt.AddMinutes(10);
        rental.ConfirmDeparture(departure, departedAt);

        Assert.Throws<InvalidOperationException>(() =>
            rental.ConfirmDeparture(departure, departedAt.AddHours(1)));

        Assert.Equal(departedAt, rental.DepartedAt);
        Assert.Equal(departure.Id, rental.DepartureInspectionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Confirmation_RejectsNullInspection(bool isReturn)
    {
        var rental = CreateRental();

        Assert.Throws<ArgumentNullException>(() =>
        {
            if (isReturn)
            {
                rental.ConfirmReturn(null!, InspectionTestData.StartedAt);
            }
            else
            {
                rental.ConfirmDeparture(null!, InspectionTestData.StartedAt);
            }
        });
    }

    [Theory]
    [InlineData(false, "wrongRental")]
    [InlineData(false, "wrongType")]
    [InlineData(false, "draft")]
    [InlineData(true, "wrongRental")]
    [InlineData(true, "wrongType")]
    [InlineData(true, "draft")]
    public void Confirmation_RejectsInvalidInspectionWithoutChangingRental(bool isReturn, string error)
    {
        var rental = CreateRental();
        var template = InspectionTestData.Template();

        if (isReturn)
        {
            var departure = InspectionTestData.Validated(template, rental.Id, InspectionType.Departure);
            rental.ConfirmDeparture(departure, InspectionTestData.StartedAt.AddMinutes(10));
        }

        var expectedType = isReturn ? InspectionType.Return : InspectionType.Departure;
        var type = error == "wrongType"
            ? (isReturn ? InspectionType.Departure : InspectionType.Return)
            : expectedType;
        Guid rentalId = error == "wrongRental" ? Guid.NewGuid() : rental.Id;
        var inspection = error == "draft"
            ? InspectionTestData.Inspection(template, rentalId, type)
            : InspectionTestData.Validated(template, rentalId, type);

        Assert.Throws<ArgumentException>(() =>
        {
            if (isReturn)
            {
                rental.ConfirmReturn(inspection, InspectionTestData.StartedAt.AddHours(1));
            }
            else
            {
                rental.ConfirmDeparture(inspection, InspectionTestData.StartedAt.AddMinutes(10));
            }
        });

        Assert.Equal(isReturn ? RentalStatus.Active : RentalStatus.Draft, rental.Status);
        Assert.Null(rental.ReturnInspectionId);
        Assert.Null(rental.ReturnedAt);

        if (!isReturn)
        {
            Assert.Null(rental.DepartureInspectionId);
            Assert.Null(rental.DepartedAt);
        }
    }

    [Fact]
    public void ReturnBeforeDeparture_IsRejected()
    {
        var rental = CreateRental();
        var inspection = InspectionTestData.Validated(
            InspectionTestData.Template(), rental.Id, InspectionType.Return
        );

        Assert.Throws<InvalidOperationException>(() =>
            rental.ConfirmReturn(inspection, InspectionTestData.StartedAt.AddHours(1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Confirmation_RejectsEarlierDateWithoutChangingState(bool isReturn)
    {
        var rental = CreateRental();
        var template = InspectionTestData.Template();
        var departedAt = InspectionTestData.StartedAt.AddMinutes(10);

        if (isReturn)
        {
            rental.ConfirmDeparture(
                InspectionTestData.Validated(template, rental.Id, InspectionType.Departure),
                departedAt
            );
        }

        var inspection = InspectionTestData.Validated(
            template, rental.Id, isReturn ? InspectionType.Return : InspectionType.Departure
        );

        Assert.Throws<ArgumentException>(() =>
        {
            if (isReturn)
            {
                rental.ConfirmReturn(inspection, departedAt.AddTicks(-1));
            }
            else
            {
                rental.ConfirmDeparture(inspection, rental.CreatedAt.AddTicks(-1));
            }
        });

        Assert.Equal(isReturn ? RentalStatus.Active : RentalStatus.Draft, rental.Status);
        Assert.Null(rental.ReturnedAt);
    }

    [Fact]
    public void EqualDates_AreAcceptedAndCompletedRentalRejectsFurtherConfirmations()
    {
        var rental = CreateRental();
        var template = InspectionTestData.Template();
        var departure = InspectionTestData.Validated(template, rental.Id, InspectionType.Departure);
        var returnInspection = InspectionTestData.Validated(template, rental.Id, InspectionType.Return);

        rental.ConfirmDeparture(departure, rental.CreatedAt);
        rental.ConfirmReturn(returnInspection, rental.CreatedAt);

        Assert.Throws<InvalidOperationException>(() =>
            rental.ConfirmReturn(returnInspection, rental.CreatedAt.AddHours(1)));
        Assert.Throws<InvalidOperationException>(() =>
            rental.ConfirmDeparture(departure, rental.CreatedAt.AddHours(1)));

        Assert.Equal(RentalStatus.Completed, rental.Status);
        Assert.Equal(rental.CreatedAt, rental.ReturnedAt);
    }
}
