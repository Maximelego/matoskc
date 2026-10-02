// Matches the current JSON contracts returned by MatosKC.Api.
export type EquipmentDto = {
  id: string;
  name: string;
  status: EquipmentStatus;
  serialNumber: string;
  equipmentCategoryId: string;
};

export type CreateEquipmentDto = {
  name: string;
  categoryId: string;
  serialNumber: string;
};

export type ListEquipmentsDto = {
  equipments: EquipmentDto[];
};

export type EquipmentStatus =
  "Available" | "Unavailable" | "ToBeDecided" | "Decommissioned" | "Maintenance" | "Borrowed";

export type EquipmentFilters = {
  categoryId?: string;
  status?: EquipmentStatus;
  search?: string;
};
