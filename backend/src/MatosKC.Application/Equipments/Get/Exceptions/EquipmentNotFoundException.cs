namespace MatosKC.Application.Equipments.Get.Exceptions;

public class EquipmentNotFoundException : Exception
{
    public EquipmentNotFoundException(Guid id) :
        base($"Equipment not found : (Id {id})")
    {
    }

    public EquipmentNotFoundException(string serialNumber) :
        base($"Equipment not found : (SerialNumber {serialNumber})")
    {
    }
}
