namespace MatosKC.Application.EquipmentCategories.Create.Exceptions;

public class EquipmentCategoryAlreadyExistsException : Exception
{
    public EquipmentCategoryAlreadyExistsException(string equipmentCategoryName) :
        base($"EquipmentCategory already exists : {equipmentCategoryName}")
    {
    }
}
