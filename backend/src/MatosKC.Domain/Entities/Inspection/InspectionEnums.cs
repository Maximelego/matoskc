namespace MatosKC.Domain.Entities.Inspection;

public enum InspectionType
{
    Departure,
    Return
}

public enum InspectionStatus
{
    Draft,
    Validated,
    Cancelled
}

public enum InspectionResult
{
    Compliant,
    NonCompliant,
}
