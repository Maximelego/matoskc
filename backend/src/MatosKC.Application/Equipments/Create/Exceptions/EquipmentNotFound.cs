namespace MatosKC.Application.Equipments;

public class EquipmentNotFound : Exception
{
    public EquipmentNotFound(Guid id) :
        base($"Equipment not found : (Id {id})")
    {
    }

    public EquipmentNotFound(string serialNumber) :
        base($"Equipment not found : (SerialNumber {serialNumber})")
    {
    }
}
