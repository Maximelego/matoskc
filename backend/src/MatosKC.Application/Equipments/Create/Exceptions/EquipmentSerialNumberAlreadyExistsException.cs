namespace MatosKC.Application.Equipments;

public class EquipmentSerialNumberAlreadyExistsException: Exception
{
    public EquipmentSerialNumberAlreadyExistsException(string serialNumber): 
        base($"This Equipment's Serial Number already exists : {serialNumber}")
    {
    }
}