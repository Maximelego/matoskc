namespace MatosKC.Infrastructure.Persistence.Seeding;

using MatosKC.Domain.Equipments;

internal static class DevelopmentData
{
    private static readonly Guid BlowingMachinesCategoryId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid ForkliftsCategoryId =
        Guid.Parse("10000000-0000-0000-0000-000000000002");

    private static readonly Guid PalletTrucksCategoryId =
        Guid.Parse("10000000-0000-0000-0000-000000000003");

    private static readonly Guid TrucksCategoryId =
        Guid.Parse("10000000-0000-0000-0000-000000000004");

    private static readonly Guid UtilityVehiclesCategoryId =
        Guid.Parse("10000000-0000-0000-0000-000000000005");

    public static IReadOnlyCollection<EquipmentCategory> CreateCategories()
    {
        return
        [
            new(
                BlowingMachinesCategoryId,
                "Machines à souffler",
                "Machines destinées à la projection et au soufflage d'isolants."
            ),
            new(
                ForkliftsCategoryId,
                "Chariots élévateurs",
                "Chariots de manutention thermiques et électriques."
            ),
            new(
                PalletTrucksCategoryId,
                "Tire-palettes",
                "Tire-palettes manuels, électriques et peseurs."
            ),
            new(
                TrucksCategoryId,
                "Camions",
                "Camions-bennes, camions plateaux et poids lourds."
            ),
            new(
                UtilityVehiclesCategoryId,
                "Véhicules utilitaires",
                "Fourgons et utilitaires légers utilisés sur les chantiers."
            )
        ];
    }

    public static IReadOnlyCollection<Equipment> CreateEquipments()
    {
        return
        [
            CreateEquipment(1, "Machine à souffler ISOVER InsulSafe", BlowingMachinesCategoryId, "DEV-SOUFFLEUR-001", EquipmentStatus.Available),
            CreateEquipment(2, "Machine à souffler Rockster II", BlowingMachinesCategoryId, "DEV-SOUFFLEUR-002", EquipmentStatus.Borrowed),
            CreateEquipment(3, "Machine à souffler Rockwool Rockster", BlowingMachinesCategoryId, "DEV-SOUFFLEUR-003", EquipmentStatus.Maintenance),

            CreateEquipment(4, "Chariot élévateur Fenwick H20D", ForkliftsCategoryId, "DEV-CHARIOT-001", EquipmentStatus.Available),
            CreateEquipment(5, "Chariot élévateur Toyota Traigo 48", ForkliftsCategoryId, "DEV-CHARIOT-002", EquipmentStatus.Unavailable),
            CreateEquipment(6, "Chariot élévateur Jungheinrich EFG 216", ForkliftsCategoryId, "DEV-CHARIOT-003", EquipmentStatus.Borrowed),

            CreateEquipment(7, "Tire-palette manuel 2,5 t", PalletTrucksCategoryId, "DEV-TIREPALETTE-001", EquipmentStatus.Available),
            CreateEquipment(8, "Tire-palette électrique Jungheinrich EJE 116", PalletTrucksCategoryId, "DEV-TIREPALETTE-002", EquipmentStatus.Maintenance),
            CreateEquipment(9, "Tire-palette peseur 2 t", PalletTrucksCategoryId, "DEV-TIREPALETTE-003", EquipmentStatus.ToBeDecided),

            CreateEquipment(10, "Camion-benne Renault Master", TrucksCategoryId, "DEV-CAMION-001", EquipmentStatus.Available),
            CreateEquipment(11, "Camion plateau Iveco Daily", TrucksCategoryId, "DEV-CAMION-002", EquipmentStatus.Borrowed),
            CreateEquipment(12, "Poids lourd Mercedes Atego", TrucksCategoryId, "DEV-CAMION-003", EquipmentStatus.Maintenance),

            CreateEquipment(13, "Fourgon Ford Transit", UtilityVehiclesCategoryId, "DEV-UTILITAIRE-001", EquipmentStatus.Available),
            CreateEquipment(14, "Fourgon Renault Trafic", UtilityVehiclesCategoryId, "DEV-UTILITAIRE-002", EquipmentStatus.Unavailable),
            CreateEquipment(15, "Fourgon Citroën Jumper", UtilityVehiclesCategoryId, "DEV-UTILITAIRE-003", EquipmentStatus.Decommissioned)
        ];
    }

    private static Equipment CreateEquipment(
        int sequence,
        string name,
        Guid categoryId,
        string serialNumber,
        EquipmentStatus status
    )
    {
        Equipment equipment = new(
            Guid.Parse($"20000000-0000-0000-0000-{sequence:D12}"),
            name,
            categoryId,
            serialNumber
        );

        ApplyStatus(equipment, status);

        return equipment;
    }

    private static void ApplyStatus(
        Equipment equipment,
        EquipmentStatus status
    )
    {
        switch (status)
        {
            case EquipmentStatus.Available:
                return;

            case EquipmentStatus.Borrowed:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                return;

            case EquipmentStatus.ToBeDecided:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                return;

            case EquipmentStatus.Unavailable:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                equipment.ChangeStatus(EquipmentStatus.Unavailable);
                return;

            case EquipmentStatus.Maintenance:
                equipment.ChangeStatus(EquipmentStatus.Borrowed);
                equipment.ChangeStatus(EquipmentStatus.ToBeDecided);
                equipment.ChangeStatus(EquipmentStatus.Unavailable);
                equipment.ChangeStatus(EquipmentStatus.Maintenance);
                return;

            case EquipmentStatus.Decommissioned:
                equipment.ChangeStatus(EquipmentStatus.Decommissioned);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}
