namespace MatosKC.Domain.Entities.Inspection;

public class InspectionAnswer: BaseEntity
{
    public Guid InspectionId { get; private set; }
    public Guid TemplatePointId { get; private set; }
    public InspectionResult Result { get; private set; }
    public string? Comment { get; private set; }
    public List<Guid> PhotosIds { get; private set; }

    public InspectionAnswer(Guid id, Guid inspectionId, Guid templatePointId, InspectionResult result, string? comment) :
    base(id) {
        InspectionId = inspectionId;
        TemplatePointId = templatePointId;
        Result = result;
        Comment = comment;
    }

}
