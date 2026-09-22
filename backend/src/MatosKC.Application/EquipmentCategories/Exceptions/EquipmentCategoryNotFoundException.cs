
public class EquipmentCategoryNotFoundException : Exception
{
    public EquipmentCategoryNotFoundException(Guid equipmentCategoryId) :
        base($"This equipment category does not exists (Id: {equipmentCategoryId})")
    {
    }
}
