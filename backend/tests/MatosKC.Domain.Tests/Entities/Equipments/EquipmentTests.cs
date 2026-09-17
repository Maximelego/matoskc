using MatosKC.Domain.Equipments;

namespace MatosKC.Domain.Tests.Equipments;

public class EquipmentTests
{
    [Fact]
    public void Constructor_WithoutId_ShouldGenerateNonEmptyId()
    {
        // Arrange et Act
        var equipment = new Equipment(
            "Fenwick 01",
            "Fenwick",
            "SN-123");

        // Assert
        Assert.NotEqual(Guid.Empty, equipment.Id);
    }

    [Fact]
    public void Constructor_WithValidValues_ShouldCreateAvailableEquipment()
    {
        var equipment = new Equipment(
            "Fenwick 01",
            "Fenwick",
            "SN-123");

        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    [Fact]
    public void Constructor_WithId_ShouldPreserveProvidedId()
    {
        var expectedId = Guid.NewGuid();

        var equipment = new Equipment(
            expectedId,
            "Fenwick 01",
            "Fenwick",
            "SN-123");

        Assert.Equal(expectedId, equipment.Id);
    }

    [Fact]
    public void Constructor_WithSurroundingSpaces_ShouldTrimTextValues()
    {
        var equipment = new Equipment(
            "  Fenwick 01  ",
            "  Fenwick  ",
            "  SN-123  ");

        Assert.Equal("Fenwick 01", equipment.Name);
        Assert.Equal("Fenwick", equipment.Category);
        Assert.Equal("SN-123", equipment.SerialNumber);
    }

    [Fact]
    public void Constructor_WithEmptyId_ShouldThrowArgumentException()
    {
        Action action = () => new Equipment(
            Guid.Empty,
            "Fenwick 01",
            "Fenwick",
            "SN-123");

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(
        string? invalidName)
    {
        Action action = () => new Equipment(
            invalidName!,
            "Fenwick",
            "SN-123");

        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCategory_ShouldThrowArgumentException(
        string? invalidCategory)
    {
        Action action = () => new Equipment(
            "Fenwick 01",
            invalidCategory!,
            "SN-123");

        Assert.ThrowsAny<ArgumentException>(action);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidSerialNumber_ShouldThrowArgumentException(
        string? invalidSerial)
    {
        Action action = () => new Equipment(
            "Fenwick 01",
            "Fenwick",
            invalidSerial!);

        Assert.ThrowsAny<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldIdentifyNameParameter()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Equipment(
                "",
                "Fenwick",
                "SN-123"));

        Assert.Equal("name", exception.ParamName);
    }

    // Status Transitions
    private static readonly HashSet<(
        EquipmentStatus Source,
        EquipmentStatus Target)> AllowedTransitionPairs =
    [
        (EquipmentStatus.Available, EquipmentStatus.Borrowed),
        (EquipmentStatus.Available, EquipmentStatus.Decommissioned),

        (EquipmentStatus.Borrowed, EquipmentStatus.Available),
        (EquipmentStatus.Borrowed, EquipmentStatus.ToBeDecided),

        (EquipmentStatus.ToBeDecided, EquipmentStatus.Available),
        (EquipmentStatus.ToBeDecided, EquipmentStatus.Unavailable),

        (EquipmentStatus.Unavailable, EquipmentStatus.Available),
        (EquipmentStatus.Unavailable, EquipmentStatus.Maintenance),
        (EquipmentStatus.Unavailable, EquipmentStatus.Decommissioned),

        (EquipmentStatus.Maintenance, EquipmentStatus.Available),
        (EquipmentStatus.Maintenance, EquipmentStatus.Unavailable),
        (EquipmentStatus.Maintenance, EquipmentStatus.Decommissioned)
    ];

    private static TheoryData<EquipmentStatus, EquipmentStatus>
        CreateTransitionData(bool allowed)
    {
        var data =
            new TheoryData<EquipmentStatus, EquipmentStatus>();

        foreach (var source in Enum.GetValues<EquipmentStatus>())
        {
            foreach (var target in Enum.GetValues<EquipmentStatus>())
            {
                var isAllowed =
                    AllowedTransitionPairs.Contains((source, target));

                if (isAllowed == allowed)
                {
                    data.Add(source, target);
                }
            }
        }

        return data;
    }

    public static TheoryData<EquipmentStatus, EquipmentStatus> ValidTransitions =>
        CreateTransitionData(allowed: true);

    public static TheoryData<EquipmentStatus, EquipmentStatus> InvalidTransitions =>
        CreateTransitionData(allowed: false);

    private static Equipment CreateEquipmentInStatus(
        EquipmentStatus expectedStatus)
    {
        var equipment = new Equipment(
            "Fenwick 01",
            "Fenwick",
            "SN-123");

        switch (expectedStatus)
        {
            case EquipmentStatus.Available:
                break;

            case EquipmentStatus.Borrowed:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                break;

            case EquipmentStatus.ToBeDecided:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                break;

            case EquipmentStatus.Unavailable:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                equipment.ChangeStatus(EquipmentStatus.Unavailable);
                break;

            case EquipmentStatus.Maintenance:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                equipment.ChangeStatus(EquipmentStatus.Unavailable);
                equipment.ChangeStatus(EquipmentStatus.Maintenance);
                break;

            case EquipmentStatus.Decommissioned:
                equipment.ChangeStatus(EquipmentStatus.Decommissioned);
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(expectedStatus),
                    expectedStatus,
                    "Unsupported equipment status.");
        }

        return equipment;
    }   


    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void ChangeStatus_WhenTransitionIsAllowed_ShouldUpdateStatus(
        EquipmentStatus sourceStatus,
        EquipmentStatus targetStatus)
    {
        var equipment = CreateEquipmentInStatus(sourceStatus);

        equipment.ChangeStatus(targetStatus);

        Assert.Equal(targetStatus, equipment.Status);
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void ChangeStatus_WhenTransitionIsForbidden_ShouldThrowAndPreserveStatus(
        EquipmentStatus sourceStatus,
        EquipmentStatus targetStatus)
    {
        var equipment = CreateEquipmentInStatus(sourceStatus);
        var initialStatus = equipment.Status;

        Assert.Throws<InvalidOperationException>(
            () => equipment.ChangeStatus(targetStatus));

        Assert.Equal(initialStatus, equipment.Status);
    }

}
