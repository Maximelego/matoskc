namespace MatosKC.Application.Inspections.Ports;

using MatosKC.Domain.Entities.Inspection;
using MatosKC.Domain.Entities.Rental;
using InspectionEntity = MatosKC.Domain.Entities.Inspection.Inspection;

public interface IInspectionReportGenerator
{
    Task<byte[]> GenerateAsync(
        InspectionEntity inspection,
        Rental rental,
        InspectionTemplate template,
        CancellationToken ct = default
    );
}

public interface IInspectionSignatureValidator
{
    void Validate(byte[] png);
}
